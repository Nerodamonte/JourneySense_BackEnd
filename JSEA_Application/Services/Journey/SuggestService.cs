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
            var remainingMinutes = (journey.TimeBudgetMinutes ?? 0) - usedMinutes;
            if (remainingMinutes <= 0) return new List<SuggestionResponse>();

            // STEP 1: Hard filter
            var alreadySuggested = await _journeyRepository.GetSuggestedExperienceIdsAsync(journeyId, cancellationToken);

            var candidates = await _experienceRepository.FindCandidatesAsync(
                vehicleType: journey.VehicleType,
                preferredCrowdLevel: journey.PreferredCrowdLevel,
                segmentPath: segment.SegmentPath,
                maxDetourDistanceMeters: journey.MaxDetourDistanceMeters,
                excludeIds: alreadySuggested,
                cancellationToken: cancellationToken);

            // Filter time budget
            candidates = candidates.Where(e =>
            {
                var detour = EstimateDetourMinutes(
                    (int)Math.Round(e.Location.Distance(segment.SegmentPath)),
                    journey.VehicleType);
                return detour <= remainingMinutes;
            }).ToList();

            if (candidates.Count == 0) return new List<SuggestionResponse>();

            // STEP 2: Build User Metadata String -> Gemini embed
            var userMetadata = BuildUserMetadataString(journey, userProfile?.TravelStyleText);
            var userVector = await EmbedTextAsync(userMetadata, cancellationToken);
            if (userVector == null) return new List<SuggestionResponse>();

            // STEP 3: Cosine search
            var candidateIds = candidates.Select(e => e.Id).ToList();
            var cosineResults = await _embeddingRepository.SearchAsync(
                userVector, candidateIds, topK: 20, cancellationToken);

            if (cosineResults.Count == 0) return new List<SuggestionResponse>();

            // STEP 4+5+6: Score + Boosts
            var realtimeWeather = await GetRealtimeWeatherStringAsync(journey, cancellationToken);
            var realtimeSeason = GetCurrentSeason();
            var realtimeTimeOfDay = GetCurrentTimeOfDay();
            var activeEventBoosts = await _experienceRepository.GetActiveEventBoostsAsync(candidateIds, cancellationToken);

            var scored = new List<(Models.Experience Exp, float Cosine, double Distance, decimal Final)>();
            var maxDetour = (double)journey.MaxDetourDistanceMeters;

            foreach (var (experienceId, cosineScore) in cosineResults)
            {
                var exp = candidates.FirstOrDefault(e => e.Id == experienceId);
                if (exp == null) continue;

                var distanceMeters = exp.Location.Distance(segment.SegmentPath);

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
                var qualityScore = exp.ExperienceMetric?.QualityScore ?? 0.5m;

                var finalSimilarity = (decimal)cosineScore
                    * (decimal)distanceScore
                    * (decimal)weatherBoost
                    * (decimal)timeBoost
                    * (decimal)seasonBoost
                    * eventBoost
                    * qualityScore;

                scored.Add((exp, cosineScore, distanceMeters, finalSimilarity));
            }

            var sorted = scored.OrderByDescending(x => x.Final).ToList();

            // STEP 7: INSERT journey_suggestions + build response
            var results = new List<SuggestionResponse>();

            foreach (var (exp, cosine, distance, final) in sorted)
            {
                var suggestion = new JourneySuggestion
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
                };

                var saved = await _journeyRepository.SaveSuggestionAsync(suggestion, cancellationToken);

                results.Add(new SuggestionResponse
                {
                    SuggestionId = saved.Id,
                    ExperienceId = exp.Id,
                    Name = exp.Name,
                    CategoryName = exp.Category?.Name,
                    Address = exp.Address,
                    City = exp.City,
                    Latitude = exp.Location.Y,
                    Longitude = exp.Location.X,
                    CoverPhotoUrl = exp.ExperiencePhotos.FirstOrDefault(p => p.IsCover == true)?.PhotoUrl,
                    PriceRange = exp.ExperienceDetail?.PriceRange,
                    AvgRating = exp.ExperienceMetric?.AvgRating,
                    DetourDistanceMeters = suggestion.DetourDistanceMeters,
                    DetourTimeMinutes = suggestion.DetourTimeMinutes,
                    CosineScore = suggestion.CosineScore,
                    DistanceScore = suggestion.DistanceScore,
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

            var prompt = BuildRagPrompt(
                expName: exp.Name,
                richDescription: exp.ExperienceDetail?.RichDescription,
                avgRating: exp.ExperienceMetric?.AvgRating,
                feedbacks: feedbacks,
                currentMood: journey.CurrentMood,
                travelStyleText: userProfile?.TravelStyleText,
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
            decimal? avgRating,
            List<string> feedbacks,
            string? currentMood,
            string? travelStyleText,
            string? weather,
            string timeOfDay,
            string season)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ban la tro ly du lich. Hay viet mot doan insight ngan (2-3 cau, tieng Viet) gioi thieu dia diem sau cho du khach.");
            sb.AppendLine();
            sb.AppendLine($"Dia diem: {expName}");
            if (!string.IsNullOrEmpty(richDescription))
                sb.AppendLine($"Mo ta: {richDescription}");
            if (avgRating.HasValue)
                sb.AppendLine($"Danh gia trung binh: {avgRating:F1}/5");
            if (feedbacks.Count > 0)
            {
                sb.AppendLine("Nhan xet tu du khach khac:");
                feedbacks.ForEach(f => sb.AppendLine($"- {f}"));
            }
            sb.AppendLine();
            sb.AppendLine("Boi canh du khach:");
            if (!string.IsNullOrEmpty(travelStyleText)) sb.AppendLine($"- Phong cach: {travelStyleText}");
            if (!string.IsNullOrEmpty(currentMood)) sb.AppendLine($"- Tam trang: {currentMood}");
            if (!string.IsNullOrEmpty(weather)) sb.AppendLine($"- Thoi tiet: {weather}");
            sb.AppendLine($"- Thoi diem trong ngay: {timeOfDay}");
            sb.AppendLine($"- Mua: {season}");
            sb.AppendLine();
            sb.AppendLine("Hay viet insight ngan gon, tu nhien, goi cam xuc. Khong liet ke, noi ro dia diem do phu hop voi so thich du lich nao, cung nhu la mood nao cua nguoi dung do, khong dung bullet point.");
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