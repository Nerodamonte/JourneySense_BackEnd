using JSEA_Application.DTOs.Request.Profile;
using JSEA_Application.DTOs.Respone.Profile;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JSEA_Application.Services.Profile
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GeminiGenerateModel = "gemini-2.5-flash";

        public UserProfileService(
            IUserProfileRepository userProfileRepository,
            IUserRepository userRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userProfileRepository = userProfileRepository;
            _userRepository = userRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task UpdateProfileAsync(
            Guid userId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Phone != null)
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null && user.Phone != request.Phone)
                {
                    user.Phone = request.Phone;
                    user.PhoneVerified = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }
            }

            var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            var isNew = profile == null;

            var hasExistingTravelStyle = profile?.TravelStyle != null && profile.TravelStyle.Count > 0;
            var incomingTravelStyle = request.TravelStyle;

            // Lần đầu bắt buộc truyền travel style để generate travel_style_text cho suggest pipeline.
            if (!hasExistingTravelStyle && (incomingTravelStyle == null || incomingTravelStyle.Count == 0))
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 travel style.");

            profile ??= new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            if (request.FullName != null)
                profile.FullName = request.FullName;

            if (request.AvatarUrl != null)
                profile.AvatarUrl = request.AvatarUrl;

            if (request.Bio != null)
                profile.Bio = request.Bio;

            if (request.AccessibilityNeeds != null)
                profile.AccessibilityNeeds = request.AccessibilityNeeds;

            // TravelStyle + TravelStyleText: chỉ cập nhật/generate khi client truyền TravelStyle và nó thực sự thay đổi.
            if (incomingTravelStyle != null)
            {
                if (incomingTravelStyle.Count == 0)
                    throw new InvalidOperationException("Vui lòng chọn ít nhất 1 travel style.");

                var incomingStrings = incomingTravelStyle
                    .Select(v => v.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var existingStrings = profile.TravelStyle ?? new List<string>();

                var incomingSet = new HashSet<string>(incomingStrings, StringComparer.OrdinalIgnoreCase);
                var existingSet = new HashSet<string>(existingStrings, StringComparer.OrdinalIgnoreCase);

                var travelStyleChanged = !incomingSet.SetEquals(existingSet);

                if (travelStyleChanged)
                {
                    profile.TravelStyle = incomingStrings;

                    var generatedText = await GenerateTravelStyleTextAsync(incomingTravelStyle, cancellationToken);
                    if (!string.IsNullOrEmpty(generatedText))
                        profile.TravelStyleText = generatedText;
                }
            }

            if (isNew)
                await _userProfileRepository.CreateAsync(profile, cancellationToken);
            else
                await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        }

       

        private async Task<string?> GenerateTravelStyleTextAsync(
            List<JSEA_Application.Enums.VibeType> travelStyle,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var styleList = string.Join(", ", travelStyle.Select(v => v.ToString()));

            var prompt = $"""
            Ban la he thong du lich. Hay viet mot doan mo ta ngan (3-4 cau, tieng Viet khong dau)
            mo ta phong cach du lich cua mot nguoi co cac so thich du lich sau: {styleList}.
            Mo ta phai the hien ro rang ho thich loai dia diem nao, khong khi nhu the nao, va trai nghiem gi, nhung dia diem do phu hop nhu the nao voi so thich du lich cua nguoi dung do.
            Chi viet doan mo ta, khong giai thich them.
            """;

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

        public async Task<ProfileResponse> GetProfileAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User không tồn tại hoặc không hợp lệ.");

            var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            var travelStyle = new List<VibeType>();
            if (profile?.TravelStyle != null)
            {
                foreach (var s in profile.TravelStyle)
                {
                    if (Enum.TryParse<VibeType>(s, ignoreCase: true, out var vibe))
                        travelStyle.Add(vibe);
                }
            }

            return new ProfileResponse
            {
                UserId = userId,
                Email = user.Email,
                Phone = user.Phone,

                FullName = profile?.FullName,
                AvatarUrl = profile?.AvatarUrl,
                Bio = profile?.Bio,
                AccessibilityNeeds = profile?.AccessibilityNeeds,

                TravelStyle = travelStyle
            };
        }
    }
}
