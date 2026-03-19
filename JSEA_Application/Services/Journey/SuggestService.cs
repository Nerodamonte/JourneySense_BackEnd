using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
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

            // Check time budget
            var usedMinutes = await _journeyRepository.GetUsedMinutesAsync(journeyId, cancellationToken);
            var baseRouteMinutes = segment.EstimatedDurationMinutes ?? journey.EstimatedDurationMinutes ?? 0;
            var remainingExtraMinutes = (journey.TimeBudgetMinutes ?? 0) - baseRouteMinutes - usedMinutes;
            if (remainingExtraMinutes <= 0) return new List<SuggestionResponse>();

            // PRODUCTION: Idempotent suggest.
            // If suggestions for this journey+segment were already generated earlier,
            // return the cached set (stable pins) instead of generating new rows.
            var cached = await _journeyRepository.GetSuggestionsByJourneySegmentAsync(journeyId, segmentId, cancellationToken);
            if (cached.Count > 0)
            {
                // If time budget has changed (waypoints accepted/removed), hide suggestions that can no longer fit.
                var filtered = cached
                    .Where(s => (s.DetourTimeMinutes ?? 0) <= remainingExtraMinutes)
                    .Take(10)
                    .ToList();

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

            // Filter time budget
            // NTS .Distance() trả về degrees, nhân 111_000 để convert sang meters
            candidates = candidates.Where(e =>
            {
                var distanceDeg = e.Location.Distance(segment.SegmentPath);
                var distanceM = (int)Math.Round(distanceDeg * 111_000);
                var detour = EstimateDetourMinutes(distanceM, journey.VehicleType);
                return detour <= remainingExtraMinutes;
            }).ToList();

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
            var scored = new List<(Models.Experience Exp, float Cosine, double Distance, decimal Final)>();
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
                var finalSimilarity = (decimal)cosineScore
                    * (decimal)distanceScore
                    * (decimal)weatherBoost
                    * (decimal)timeBoost
                    * (decimal)seasonBoost
                    * eventBoost;

                scored.Add((exp, cosineScore, distanceMeters, finalSimilarity));
            }

            var sorted = scored
                .OrderByDescending(x => x.Final)
                .Take(10)
                .ToList();

            // STEP 7: INSERT journey_suggestions (batch) + build response
            var suggestionsToSave = new List<JourneySuggestion>(sorted.Count);
            foreach (var (exp, cosine, distance, final) in sorted)
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
            foreach (var (exp, _, _, final) in sorted)
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

            await _journeyRepository.UpdateSuggestionInsightAsync(suggestionId, insight, cancellationToken);

            return insight;
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
            if (!string.IsNullOrEmpty(journey.CurrentMood))
                parts.Add($"Tam trang hien tai: {journey.CurrentMood}");
            if (!string.IsNullOrEmpty(journey.VehicleType))
                parts.Add($"Phuong tien: {journey.VehicleType}");
            return string.Join(". ", parts);
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

            // === DU LIEU DAU VAO ===
            sb.AppendLine("=== THONG TIN DIA DIEM ===");
            sb.AppendLine($"Ten: {expName}");
            if (!string.IsNullOrEmpty(richDescription))
                sb.AppendLine($"Mo ta: {richDescription}");
            if (!string.IsNullOrEmpty(priceRange))
                sb.AppendLine($"Gia: {priceRange}");
            if (!string.IsNullOrEmpty(crowdLevel))
                sb.AppendLine($"Muc do dong duc: {crowdLevel}");
            sb.AppendLine();
            sb.AppendLine("=== BOI CANH NGUOI DUNG ===");
            if (!string.IsNullOrEmpty(travelStyleList))
                sb.AppendLine($"Cac vibe du lich cu the cua nguoi dung (liet ke day du): {travelStyleList}");
            if (!string.IsNullOrEmpty(travelStyleText))
                sb.AppendLine($"Mo ta phong cach du lich: {travelStyleText}");
            if (!string.IsNullOrEmpty(currentMood))
                sb.AppendLine($"Tam trang: {currentMood}");
            if (!string.IsNullOrEmpty(vehicleType))
                sb.AppendLine($"Phuong tien: {vehicleType}");
            if (detourDistanceMeters.HasValue)
            {
                var detourM = detourDistanceMeters.Value;
                string detourNote;
                if (detourM <= 300)
                    detourNote = $"Do lech khoi tuyen chinh: {detourM}m — rat gan, gan nhu khong can di lech, rat dang ghe";
                else if (detourM <= 800)
                    detourNote = $"Do lech khoi tuyen chinh: {detourM}m — lech nhe, di them vai phut la toi, kha thuan tien";
                else if (detourM <= 2000)
                    detourNote = $"Do lech khoi tuyen chinh: {detourM}m — lech khoang 1-2km, can can nhac neu khong co nhieu thoi gian";
                else
                    detourNote = $"Do lech khoi tuyen chinh: {detourM}m — lech kha xa so voi tuyen, nen can nhac ky truoc khi quyet dinh ghe, chi nen di neu dia diem nay that su phu hop voi ban";
                sb.AppendLine(detourNote);
            }
            if (!string.IsNullOrEmpty(weather))
                sb.AppendLine($"Thoi tiet: {weather}");
            sb.AppendLine($"Thoi diem trong ngay: {timeOfDay}");
            sb.AppendLine($"Mua hien tai: {season}");
            sb.AppendLine();

            // === INSTRUCTION CHAT CHE ===
            sb.AppendLine("=== NHIEM VU ===");
            sb.AppendLine("Viet mot doan van 2-3 cau tieng Viet co dau, tu nhien nhu nguoi ban noi chuyen.");
            sb.AppendLine("Doan van BUOC PHAI de cap day du TAT CA cac yeu to sau (theo dung thu tu nay):");
            sb.AppendLine("1. Phong cach du lich va tam trang cua nguoi dung co phu hop voi dia diem nay nhu the nao");
            sb.AppendLine("2. Thoi tiet va thoi diem trong ngay co thuan loi de ghe khong");
            sb.AppendLine("3. Gia ca va muc do dong duc co phu hop voi nguoi dung khong");
            sb.AppendLine("4. Do lech so voi tuyen di — neu ro la chi can lech [so met] la toi duoc, gan hay xa, co dang ghé khong");
            sb.AppendLine("Khong duoc bo qua bat ky yeu to nao trong 4 yeu to tren.");
            sb.AppendLine("TUYET DOI KHONG in ra so thu tu, bullet point, tag, hay bat ky chu giai nao. Chi co doan van thuan tuy.");
            return sb.ToString();
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