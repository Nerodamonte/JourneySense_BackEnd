using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Application.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace JSEA_Application.Services.Journey
{
    public class SuggestService : ISuggestService
    {
        private readonly IJourneyRepository _journeyRepository;
        private readonly IMicroExperienceRepository _experienceRepository;
        private readonly IExperienceEmbeddingRepository _embeddingRepository;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IWeatherService _weatherService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GeminiEmbedModel = "gemini-embedding-001";
        private const string GeminiGenerateModel = "gemini-2.5-flash";

        // Mood tuning: keep profile dominant, mood nudges ranking.
        private const decimal MoodBoostHappy = 1.15m;
        private const decimal MoodBoostSad = 1.20m;
        private const decimal MoodBoostStressed = 1.25m;

        // Diversity guard: ensure at least this many mood-aligned suggestions (if available).
        private const int MinMoodAlignedDefault = 3;
        private const int MinMoodAlignedStressed = 4;

        public SuggestService(
            IJourneyRepository journeyRepository,
            IMicroExperienceRepository experienceRepository,
            IExperienceEmbeddingRepository embeddingRepository,
            IFeedbackRepository feedbackRepository,
            IUserProfileRepository userProfileRepository,
            IWeatherService weatherService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _journeyRepository = journeyRepository;
            _experienceRepository = experienceRepository;
            _embeddingRepository = embeddingRepository;
            _feedbackRepository = feedbackRepository;
            _userProfileRepository = userProfileRepository;
            _weatherService = weatherService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // =========================================================
        // PUBLIC: GetSuggestionsAsync
        // =========================================================

        public async Task<List<SuggestionResponse>> GetSuggestionsAsync(
            Guid journeyId,
            Guid segmentId,
            CancellationToken cancellationToken = default)
        {
            var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
            if (journey == null) return new List<SuggestionResponse>();

            var segment = journey.RouteSegments.FirstOrDefault(s => s.Id == segmentId);
            if (segment?.SegmentPath == null) return new List<SuggestionResponse>();

            var userProfile = await _userProfileRepository.GetByUserIdAsync(journey.TravelerId, cancellationToken);

            // Check max_stops
            var acceptedCount = await _journeyRepository.GetAcceptedWaypointCountAsync(journeyId, cancellationToken);
            if (journey.MaxStops.HasValue && acceptedCount >= journey.MaxStops.Value)
                return new List<SuggestionResponse>();

            // Thời gian còn lại cho dừng/khám phá: TimeBudgetMinutes − Σ ActualStopMinutes (waypoint đã checkout).
            var usedMinutes = await _journeyRepository.GetUsedMinutesAsync(journeyId, cancellationToken);
            var remainingExploreMinutes = (journey.TimeBudgetMinutes ?? 0) - usedMinutes;
            if (remainingExploreMinutes <= 0) return new List<SuggestionResponse>();

            // PRODUCTION: Idempotent suggest.
            // If suggestions for this journey+segment were already generated earlier,
            // return the cached set (stable pins) instead of generating new rows.
            var cached = await _journeyRepository.GetSuggestionsByJourneySegmentAsync(journeyId, segmentId, cancellationToken);
            if (cached.Count > 0)
            {
                var filtered = cached.Take(10).ToList();

                return filtered.Select(s => new SuggestionResponse
                {
                    SuggestionId = s.Id,
                    ExperienceId = s.ExperienceId,
                    SegmentId = s.SegmentId,
                    Name = s.Experience?.Name,
                    CategoryName = s.Experience?.Category?.Name,
                    Address = s.Experience?.Address,
                    City = s.Experience?.City,
                    Latitude = s.Experience?.Location?.Y,
                    Longitude = s.Experience?.Location?.X,
                    CoverPhotoUrl = s.Experience?.ExperiencePhotos?.FirstOrDefault(p => p.IsCover == true)?.PhotoUrl,
                    PriceRange = s.Experience?.ExperienceDetail?.PriceRange,
                    CrowdLevel = s.Experience?.ExperienceDetail?.CrowdLevel,
                    OpeningHours = s.Experience?.ExperienceDetail?.OpeningHours,
                    AccessibleBy = s.Experience?.AccessibleBy,
                    AvgRating = s.Experience?.ExperienceMetric?.AvgRating,
                    TotalRatings = s.Experience?.ExperienceMetric?.TotalRatings,
                    DetourDistanceMeters = s.DetourDistanceMeters,
                    DetourTimeMinutes = s.DetourTimeMinutes,
                    CosineScore = s.CosineScore,
                    DistanceScore = s.DistanceScore,
                    FinalSimilarity = s.FinalSimilarity,
                    AiInsight = s.AiInsight
                }).ToList();
            }

            // STEP 1: Hard filter
            var alreadySuggested = await _journeyRepository.GetSuggestedExperienceIdsAsync(journeyId, segmentId, cancellationToken);

            var candidates = await _experienceRepository.FindCandidatesAsync(
                vehicleType: journey.VehicleType,
                preferredCrowdLevel: journey.PreferredCrowdLevel,
                segmentPath: segment.SegmentPath,
                maxDetourDistanceMeters: journey.MaxDetourDistanceMeters,
                excludeIds: alreadySuggested,
                cancellationToken: cancellationToken);

            if (candidates.Count == 0) return new List<SuggestionResponse>();

            // STEP 2: Build User Metadata String -> Gemini embed
            var userMetadata = BuildUserMetadataString(journey, userProfile?.TravelStyleText);
            var userVector = await EmbedTextAsync(userMetadata, cancellationToken);
            if (userVector == null) return new List<SuggestionResponse>();

            // STEP 3: Cosine search
            var candidateIds = candidates.Select(e => e.Id).ToList();
            var cosineResults = await _embeddingRepository.GetCosineScoresAsync(
                userVector, candidateIds, cancellationToken);

            if (cosineResults.Count == 0) return new List<SuggestionResponse>();

            // STEP 4+5+6: Score + Boosts
            var realtimeWeather = await GetRealtimeWeatherStringAsync(journey, cancellationToken);
            var realtimeSeason = GetCurrentSeason();
            var realtimeTimeOfDay = GetCurrentTimeOfDay();
            var activeEventBoosts = await _experienceRepository.GetActiveEventBoostsAsync(candidateIds, cancellationToken);

            var candidatesById = candidates.ToDictionary(e => e.Id, e => e);
            var mood = ParseMood(journey.CurrentMood);
            var moodVibeTags = mood.HasValue ? MapMoodToVibeTags(mood.Value) : Array.Empty<string>();
            var scored = new List<(Models.Experience Exp, float Cosine, double Distance, decimal Final, bool MoodAligned)>();
            var maxDetour = (double)journey.MaxDetourDistanceMeters;

            foreach (var (experienceId, cosineScore) in cosineResults)
            {
                if (!candidatesById.TryGetValue(experienceId, out var exp))
                    continue;

                // NTS .Distance() trả về degrees, nhân 111_000 để convert sang meters
                var distanceMeters = exp.Location.Distance(segment.SegmentPath) * 111_000;

                // Normalize distance score về [0, 1]:
                // score = maxDetour / (maxDetour + distance)
                // → distance = 0   → score = 1.0 (tốt nhất)
                // → distance = max → score = 0.5
                // → luôn trong [0,1], không bao giờ overflow numeric(5,4)
                var distanceScore = maxDetour / (maxDetour + distanceMeters);

                var weatherBoost = GetWeatherBoost(exp, realtimeWeather);
                var timeBoost = GetTimeBoost(exp, realtimeTimeOfDay);
                var seasonBoost = GetSeasonBoost(exp, realtimeSeason);
                var eventBoost = activeEventBoosts.TryGetValue(experienceId, out var eb) ? eb : 1.0m;

                var moodAligned = moodVibeTags.Length > 0 && HasAnyTag(exp, moodVibeTags);
                var moodBoost = moodAligned ? GetMoodBoost(mood) : 1.0m;

                var finalSimilarity = (decimal)cosineScore
                    * (decimal)distanceScore
                    * (decimal)weatherBoost
                    * (decimal)timeBoost
                    * (decimal)seasonBoost
                    * eventBoost
                    * moodBoost;

                scored.Add((exp, cosineScore, distanceMeters, finalSimilarity, moodAligned));
            }

            var sortedAll = scored
                .OrderByDescending(x => x.Final)
                .ToList();

            var sorted = TakeTopWithMoodDiversity(sortedAll, mood);

            // STEP 7: INSERT journey_suggestions (batch) + build response
            var suggestionsToSave = new List<JourneySuggestion>(sorted.Count);
            foreach (var (exp, cosine, distance, final, _) in sorted)
            {
                suggestionsToSave.Add(new JourneySuggestion
                {
                    Id = Guid.NewGuid(),
                    JourneyId = journeyId,
                    ExperienceId = exp.Id,
                    SegmentId = segmentId,
                    DetourDistanceMeters = (int)Math.Round(distance),
                    DetourTimeMinutes = EstimateDetourMinutes((int)Math.Round(distance), journey.VehicleType),
                    CosineScore = (decimal)cosine,
                    DistanceScore = (decimal)(maxDetour / (maxDetour + distance)),
                    FinalSimilarity = final,
                    AiInsight = null,
                    SuggestedAt = DateTime.UtcNow
                });
            }

            await _journeyRepository.SaveSuggestionsAsync(suggestionsToSave, cancellationToken);

            var suggestionsByExpId = suggestionsToSave.ToDictionary(s => s.ExperienceId, s => s);
            var results = new List<SuggestionResponse>(sorted.Count);
            foreach (var (exp, _, _, final, _) in sorted)
            {
                if (!suggestionsByExpId.TryGetValue(exp.Id, out var s))
                    continue;

                results.Add(new SuggestionResponse
                {
                    SuggestionId = s.Id,
                    ExperienceId = exp.Id,
                    SegmentId = segmentId,
                    Name = exp.Name,
                    CategoryName = exp.Category?.Name,
                    Address = exp.Address,
                    City = exp.City,
                    Latitude = exp.Location.Y,
                    Longitude = exp.Location.X,
                    CoverPhotoUrl = exp.ExperiencePhotos.FirstOrDefault(p => p.IsCover == true)?.PhotoUrl,
                    PriceRange = exp.ExperienceDetail?.PriceRange,
                    CrowdLevel = exp.ExperienceDetail?.CrowdLevel,
                    OpeningHours = exp.ExperienceDetail?.OpeningHours,
                    AccessibleBy = exp.AccessibleBy,
                    AvgRating = exp.ExperienceMetric?.AvgRating,
                    TotalRatings = exp.ExperienceMetric?.TotalRatings,
                    DetourDistanceMeters = s.DetourDistanceMeters,
                    DetourTimeMinutes = s.DetourTimeMinutes,
                    CosineScore = s.CosineScore,
                    DistanceScore = s.DistanceScore,
                    FinalSimilarity = final,
                    AiInsight = null
                });
            }

            return results;
        }

        // =========================================================
        // PUBLIC: GetAiInsightAsync — RAG on-demand
        // =========================================================

        public async Task<string?> GetAiInsightAsync(
            Guid suggestionId,
            CancellationToken cancellationToken = default)
        {
            var suggestion = await _journeyRepository.GetSuggestionByIdAsync(suggestionId, cancellationToken);
            if (suggestion == null) return null;

            if (!string.IsNullOrEmpty(suggestion.AiInsight))
                return suggestion.AiInsight;

            var exp = suggestion.Experience;
            var journey = suggestion.Journey;

            var feedbacks = await _feedbackRepository.GetTopByExperienceIdAsync(exp.Id, topN: 3, cancellationToken);
            var userProfile = await _userProfileRepository.GetByUserIdAsync(journey.TravelerId, cancellationToken);
            var realtimeWeather = await GetRealtimeWeatherStringAsync(journey, cancellationToken);

            // Map vibe enum sang tiếng Việt để AI insight tự nhiên hơn
            static string MapVibeToVietnamese(string vibe) => vibe.ToLower() switch
            {
                "chill" => "thích không gian yên tĩnh, thư thái",
                "relax" => "thích nghỉ ngơi, thư giãn",
                "explorer" => "thích khám phá, tìm hiểu",
                "foodie" => "mê ẩm thực, thích thử đồ ăn mới",
                "localvibes" => "thích trải nghiệm văn hóa địa phương",
                "adventure" => "thích mạo hiểm, trải nghiệm mới lạ",
                "photographer" => "thích chụp ảnh, săn góc đẹp",
                _ => vibe
            };

            var travelStyleList = userProfile?.TravelStyle != null && userProfile.TravelStyle.Count > 0
                ? string.Join(", ", userProfile.TravelStyle.Select(MapVibeToVietnamese))
                : null;

            var prompt = BuildRagPrompt(
                expName: exp.Name,
                richDescription: exp.ExperienceDetail?.RichDescription,
                priceRange: exp.ExperienceDetail?.PriceRange,
                crowdLevel: exp.ExperienceDetail?.CrowdLevel,
                detourDistanceMeters: suggestion.DetourDistanceMeters,
                currentMood: journey.CurrentMood,
                travelStyleList: travelStyleList,
                travelStyleText: userProfile?.TravelStyleText,
                vehicleType: journey.VehicleType,
                weather: realtimeWeather,
                timeOfDay: GetCurrentTimeOfDay(),
                season: GetCurrentSeason()
            );

            var insight = await GenerateTextAsync(prompt, cancellationToken);
            if (string.IsNullOrEmpty(insight)) return null;

            insight = NormalizeInsightText(insight);

            await _journeyRepository.UpdateSuggestionInsightAsync(suggestionId, insight, cancellationToken);

            return insight;
        }

        public async Task<SuggestionCommunityResponse?> GetSuggestionCommunityAsync(
            Guid suggestionId,
            Guid viewerTravelerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var suggestion = await _journeyRepository.GetSuggestionByIdAsync(suggestionId, cancellationToken);
            if (suggestion?.Journey == null)
                return null;

            if (suggestion.Journey.TravelerId != viewerTravelerId)
                return null;

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var exp = suggestion.Experience;
            var m = exp.ExperienceMetric;

            // Không loại feedback của chính viewer: “cộng đồng” = mọi bình luận đã duyệt tại địa điểm,
            // kể cả của người đang xem (tránh list rỗng khi chỉ có 1 người đã review).
            var (items, total) = await _feedbackRepository.ListPublicApprovedForExperienceAsync(
                exp.Id,
                excludeTravelerId: null,
                page,
                pageSize,
                cancellationToken);

            static string? DisplayName(UserProfile? p)
            {
                var n = p?.FullName;
                return string.IsNullOrWhiteSpace(n) ? null : n.Trim();
            }

            var feedbacks = items
                .Select(f => new PublicExperienceFeedbackItemDto
                {
                    FeedbackId = f.Id,
                    Text = f.FeedbackText,
                    CreatedAt = f.CreatedAt,
                    Stars = f.Visit?.Rating != null ? f.Visit.Rating.Rating1 : null,
                    AuthorDisplayName = DisplayName(f.Visit?.Traveler?.UserProfile)
                })
                .ToList();

            return new SuggestionCommunityResponse
            {
                ExperienceId = exp.Id,
                ExperienceName = exp.Name,
                Metrics = new ExperienceSocialMetricsDto
                {
                    TotalVisits = m?.TotalVisits,
                    TotalRatings = m?.TotalRatings,
                    AvgRating = m?.AvgRating
                },
                Feedbacks = feedbacks,
                Page = page,
                PageSize = pageSize,
                TotalFeedbacks = total
            };
        }

        // =========================================================
        // PRIVATE: Gemini Embed
        // =========================================================

        private async Task<Pgvector.Vector?> EmbedTextAsync(string text, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var client = _httpClientFactory.CreateClient();
            var url =
$"https://generativelanguage.googleapis.com/v1beta/models/{GeminiEmbedModel}:embedContent?key={apiKey}";

            var body = JsonSerializer.Serialize(new
            {
                model = $"models/{GeminiEmbedModel}",
                content = new { parts = new[] { new { text } } }
            });

            var response = await client.PostAsync(url,
                new StringContent(body, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("embedding", out var embObj)) return null;
            if (!embObj.TryGetProperty("values", out var valuesEl)) return null;

            var floats = valuesEl.EnumerateArray().Select(v => v.GetSingle()).ToArray();
            return new Pgvector.Vector(floats);
        }

        // =========================================================
        // PRIVATE: Gemini Generate (RAG)
        // =========================================================

        private async Task<string?> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiGenerateModel}:generateContent?key={apiKey}";

            var body = JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                new { parts = new[] { new { text = prompt } } }
            }
            });

            var response = await client.PostAsync(url,
                new StringContent(body, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }

        // =========================================================
        // PRIVATE: Helpers
        // =========================================================

        private static string BuildUserMetadataString(Models.Journey journey, string? travelStyleText)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(travelStyleText))
                parts.Add(travelStyleText);
            // IMPORTANT: Do not embed MoodType (Happy/Sad/Stressed...) directly.
            // Mood is handled as a rule-based boost on top of cosine similarity to preserve profile dominance
            // and align with vibe tags (Chill/Relax/Explorer/Foodie/...).
            if (!string.IsNullOrEmpty(journey.VehicleType))
                parts.Add($"Phuong tien: {journey.VehicleType}");
            return string.Join(". ", parts);
        }

        private static MoodType? ParseMood(string? currentMood)
        {
            if (string.IsNullOrWhiteSpace(currentMood))
                return null;

            if (!Enum.TryParse<MoodType>(currentMood, ignoreCase: true, out var m))
                return null;

            // Treat Normal as baseline (no mood boost / no mood quota).
            return m == MoodType.Normal ? null : m;
        }

        private static decimal GetMoodBoost(MoodType? mood)
        {
            return mood switch
            {
                MoodType.Happy => MoodBoostHappy,
                MoodType.Sad => MoodBoostSad,
                MoodType.Stressed => MoodBoostStressed,
                _ => 1.0m
            };
        }

        private static string[] MapMoodToVibeTags(MoodType mood)
        {
            // Map real-time MoodType to vibe tokens that exist in Experience.Tags.
            return mood switch
            {
                MoodType.Stressed => new[] { "Relax", "Chill" },
                MoodType.Sad => new[] { "Chill", "Relax", "LocalVibes" },
                MoodType.Happy => new[] { "Adventure", "Explorer", "Foodie" },
                // Normal means no extra vibe push.
                MoodType.Normal => Array.Empty<string>(),
                _ => Array.Empty<string>()
            };
        }

        private static bool HasAnyTag(Models.Experience exp, IEnumerable<string> required)
        {
            if (exp.Tags == null || exp.Tags.Count == 0)
                return false;

            var set = new HashSet<string>(exp.Tags.Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase);
            foreach (var r in required)
            {
                if (set.Contains(r))
                    return true;
            }

            return false;
        }

        private static List<(Models.Experience Exp, float Cosine, double Distance, decimal Final, bool MoodAligned)> TakeTopWithMoodDiversity(
            List<(Models.Experience Exp, float Cosine, double Distance, decimal Final, bool MoodAligned)> sortedAll,
            MoodType? mood)
        {
            var top = sortedAll.Take(10).ToList();

            if (!mood.HasValue)
                return top;

            var minAligned = mood.Value == MoodType.Stressed ? MinMoodAlignedStressed : MinMoodAlignedDefault;
            var alignedCount = top.Count(x => x.MoodAligned);
            if (alignedCount >= minAligned)
                return top;

            // Pull best mood-aligned items (if any) and swap out lowest non-aligned items.
            foreach (var candidate in sortedAll.Where(x => x.MoodAligned))
            {
                if (top.Any(x => x.Exp.Id == candidate.Exp.Id))
                    continue;

                top.Add(candidate);

                // Remove one lowest-scoring non-aligned item to keep size <= 10.
                var remove = top
                    .Where(x => !x.MoodAligned)
                    .OrderBy(x => x.Final)
                    .FirstOrDefault();

                if (remove.Exp != null && top.Count > 10)
                    top.Remove(remove);

                alignedCount = top.Count(x => x.MoodAligned);
                if (alignedCount >= minAligned && top.Count == 10)
                    break;
            }

            return top
                .OrderByDescending(x => x.Final)
                .Take(10)
                .ToList();
        }

        private static string BuildRagPrompt(
            string expName,
            string? richDescription,
            string? priceRange,
            string? crowdLevel,
            int? detourDistanceMeters,
            string? currentMood,
            string? travelStyleList,
            string? travelStyleText,
            string? vehicleType,
            string? weather,
            string timeOfDay,
            string season)
        {
            var sb = new StringBuilder();

            static string MapMoodToVietnamese(string? mood) => (mood ?? "").Trim().ToLowerInvariant() switch
            {
                "happy" => "đang vui / có năng lượng",
                "normal" => "tâm trạng bình thường",
                "sad" => "đang hơi buồn",
                "stressed" => "đang căng thẳng",
                _ => string.IsNullOrWhiteSpace(mood) ? "" : mood.Trim()
            };

            static string MapTimeOfDayToVietnamese(string t) => (t ?? "").Trim() switch
            {
                "Morning" => "buổi sáng",
                "Afternoon" => "buổi chiều",
                "Evening" => "buổi tối",
                "Night" => "ban đêm",
                _ => string.IsNullOrWhiteSpace(t) ? "" : t
            };

            static string MapSeasonToVietnamese(string s) => (s ?? "").Trim() switch
            {
                "Spring" => "mùa xuân",
                "Summer" => "mùa hè",
                "Autumn" => "mùa thu",
                "Winter" => "mùa đông",
                _ => string.IsNullOrWhiteSpace(s) ? "" : s
            };

            static string MapWeatherToVietnamese(string? w) => (w ?? "").Trim().ToLowerInvariant() switch
            {
                "sunny" => "trời nắng",
                "clear" => "trời quang",
                "cloudy" => "trời nhiều mây",
                "overcast" => "trời âm u",
                "rainy" => "trời mưa",
                "storm" or "stormy" => "trời giông",
                "drizzle" => "mưa lất phất",
                "fog" or "foggy" => "trời có sương",
                _ => string.IsNullOrWhiteSpace(w) ? "" : w.Trim()
            };

            static string MapCrowdLevelToVietnamese(string? c) => (c ?? "").Trim().ToLowerInvariant() switch
            {
                "quiet" => "địa điểm này khá vắng khách",
                "normal" => "địa điểm này không đông khách lắm",
                "busy" => "địa điểm này khá đông khách",
                "vắng" => "địa điểm này khá vắng khách",
                "đông" => "địa điểm này khá đông khách",
                _ => string.IsNullOrWhiteSpace(c) ? "" : c.Trim()
            };

            var moodVi = MapMoodToVietnamese(currentMood);
            var timeOfDayVi = MapTimeOfDayToVietnamese(timeOfDay);
            var seasonVi = MapSeasonToVietnamese(season);
            var weatherVi = MapWeatherToVietnamese(weather);
            var crowdLevelVi = MapCrowdLevelToVietnamese(crowdLevel);

            // === DỮ LIỆU ĐẦU VÀO (FACTS) ===
            sb.AppendLine("=== THÔNG TIN ĐỊA ĐIỂM (FACTS) ===");
            sb.AppendLine($"Tên: {expName}");
            if (!string.IsNullOrEmpty(richDescription))
                sb.AppendLine($"Mô tả: {richDescription}");
            if (!string.IsNullOrEmpty(priceRange))
                sb.AppendLine($"Giá: {priceRange}");
            if (!string.IsNullOrEmpty(crowdLevelVi))
                sb.AppendLine($"Tình trạng khách: {crowdLevelVi}");
            sb.AppendLine();
            sb.AppendLine("=== BỐI CẢNH NGƯỜI DÙNG (FACTS) ===");
            if (!string.IsNullOrEmpty(travelStyleList))
                sb.AppendLine($"Vibe du lịch (liệt kê): {travelStyleList}");
            if (!string.IsNullOrEmpty(travelStyleText))
                sb.AppendLine($"Mô tả phong cách: {travelStyleText}");
            if (!string.IsNullOrEmpty(moodVi))
                sb.AppendLine($"Tâm trạng hiện tại: {moodVi}");
            if (!string.IsNullOrEmpty(vehicleType))
                sb.AppendLine($"Phương tiện: {vehicleType}");
            if (detourDistanceMeters.HasValue)
            {
                var detourM = detourDistanceMeters.Value;
                string detourNote;
                if (detourM <= 300)
                    detourNote = $"Độ lệch khỏi tuyến chính: {detourM}m — rất gần";
                else if (detourM <= 800)
                    detourNote = $"Độ lệch khỏi tuyến chính: {detourM}m — lệch nhẹ, khá thuận tiện";
                else if (detourM <= 2000)
                    detourNote = $"Độ lệch khỏi tuyến chính: {detourM}m — khoảng 1–2km, nên cân nhắc thời gian";
                else
                    detourNote = $"Độ lệch khỏi tuyến chính: {detourM}m — lệch khá xa, chỉ nên ghé nếu thật sự hợp";
                sb.AppendLine(detourNote);
            }
            if (!string.IsNullOrEmpty(weatherVi))
                sb.AppendLine($"Thời tiết: {weatherVi}");
            if (!string.IsNullOrEmpty(timeOfDayVi))
                sb.AppendLine($"Thời điểm trong ngày: {timeOfDayVi}");
            if (!string.IsNullOrEmpty(seasonVi))
                sb.AppendLine($"Mùa: {seasonVi}");
            sb.AppendLine();

            // === INSTRUCTION: highlight keys, dễ quét mắt ===
            sb.AppendLine("=== NHIỆM VỤ ===");
            sb.AppendLine("Bạn là người bạn đồng hành. Tiếng Việt tự nhiên, xưng hô 'bạn'.");
            sb.AppendLine("Định dạng BẮT BUỘC: đúng 4 dòng, mỗi dòng bắt đầu bằng '• ' (bullet + khoảng trắng). Mỗi dòng = một highlight ngắn (khoảng 1 câu, tối đa ~150 ký tự), không viết đoạn văn nối dài nhiều câu.");
            sb.AppendLine("Giữ đúng thứ tự 4 dòng sau (mỗi dòng một ý, lược mà đủ ý):");
            sb.AppendLine("Dòng 1 — Phong cách du lịch + tâm trạng hiện tại có hợp với địa điểm này không (không phán xét quá đà).");
            sb.AppendLine("Dòng 2 — Thời tiết + thời điểm trong ngày: có thuận để ghé không.");
            sb.AppendLine("Dòng 3 — Giá + đông/vắng (chỉ nếu có trong FACTS). Viết 'địa điểm này ...' hoặc 'quán này ...'; không dùng cụm 'mức độ ...'; giữ đúng số/đơn vị từ FACTS.");
            sb.AppendLine("Dòng 4 — Độ lệch tuyến: nêu đúng số mét nếu FACTS có (vd. lệch 61m), kết bằng một gợi ý nhẹ.");
            sb.AppendLine("Không thêm dòng thứ 5; không tiêu đề / không đánh số 1. ngoài bullet • ; không markdown đậm.");
            sb.AppendLine("CHỈ dùng thông tin trong FACTS, không bịa thêm chi tiết.");
            sb.AppendLine("TUYỆT ĐỐI không dùng quiet, normal, busy; không dùng cụm 'mức độ ...'.");
            return sb.ToString();
        }

        private static string NormalizeInsightText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = raw.Trim();

            // Remove markdown fences if the model returns them.
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline >= 0)
                    text = text[(firstNewline + 1)..];

                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                    text = text[..lastFence];

                text = text.Trim();
            }

            static string CollapseSpaces(string line) =>
                string.Join(' ', line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            // Giữ xuống dòng giữa các highlight; thu gọn khoảng trắng trong từng dòng.
            var lines = text
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(CollapseSpaces)
                .Where(l => l.Length > 0)
                .ToList();
            text = lines.Count > 0
                ? string.Join('\n', lines)
                : CollapseSpaces(text);

            // Guardrail: if the model still outputs 'mức độ ...', rewrite to natural subject phrasing.
            text = text.Replace("mức độ không đông khách lắm", "địa điểm này không đông khách lắm", StringComparison.OrdinalIgnoreCase);
            text = text.Replace("mức độ khá vắng khách", "địa điểm này khá vắng khách", StringComparison.OrdinalIgnoreCase);
            text = text.Replace("mức độ khá đông khách", "địa điểm này khá đông khách", StringComparison.OrdinalIgnoreCase);

            return text;
        }

        private async Task<string?> GetRealtimeWeatherStringAsync(Models.Journey? journey, CancellationToken cancellationToken)
        {
            if (journey?.OriginLocation == null) return null;
            var weather = await _weatherService.GetCurrentWeatherAsync(
                journey.OriginLocation.Y, journey.OriginLocation.X, cancellationToken);
            return weather?.WeatherType.ToString();
        }

        private static double GetWeatherBoost(Models.Experience exp, string? realtimeWeather)
        {
            if (string.IsNullOrEmpty(realtimeWeather)) return 1.0;
            if (exp.WeatherSuitability == null || exp.WeatherSuitability.Count == 0) return 1.0;
            return exp.WeatherSuitability.Contains(realtimeWeather, StringComparer.OrdinalIgnoreCase) ? 1.2 : 1.0;
        }

        private static double GetTimeBoost(Models.Experience exp, string timeOfDay)
        {
            if (exp.PreferredTimes == null || exp.PreferredTimes.Count == 0) return 1.0;
            return exp.PreferredTimes.Contains(timeOfDay, StringComparer.OrdinalIgnoreCase) ? 1.2 : 1.0;
        }

        private static double GetSeasonBoost(Models.Experience exp, string season)
        {
            if (exp.Seasonality == null || exp.Seasonality.Count == 0) return 1.0;
            if (exp.Seasonality.Contains("YearRound", StringComparer.OrdinalIgnoreCase)) return 1.2;
            return exp.Seasonality.Contains(season, StringComparer.OrdinalIgnoreCase) ? 1.2 : 1.0;
        }

        private static int EstimateDetourMinutes(int detourMeters, string vehicleType)
        {
            var speedKmh = vehicleType.ToLowerInvariant() switch
            {
                "walking" => 5.0,
                "bicycle" => 15.0,
                "motorbike" => 35.0,
                "car" => 40.0,
                _ => 30.0
            };
            return (int)Math.Ceiling(detourMeters / 1000.0 / speedKmh * 60);
        }

        private static string GetCurrentTimeOfDay()
        {
            var hour = DateTime.UtcNow.AddHours(7).Hour;
            return hour switch
            {
                >= 5 and < 12 => "Morning",
                >= 12 and < 17 => "Afternoon",
                >= 17 and < 21 => "Evening",
                _ => "Night"
            };
        }

        private static string GetCurrentSeason()
        {
            var month = DateTime.UtcNow.AddHours(7).Month;
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                _ => "Autumn"
            };
        }
    }
}