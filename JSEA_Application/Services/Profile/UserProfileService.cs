using JSEA_Application.DTOs.Request.Profile;
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
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GeminiGenerateModel = "gemini-2.5-flash";

        public UserProfileService(
            IUserProfileRepository userProfileRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userProfileRepository = userProfileRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task UpdateProfileAsync(
            Guid userId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            // Lấy hoặc tạo profile
            var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            var isNew = profile == null;

            profile ??= new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            // Cập nhật các field thông thường (chỉ override nếu request có giá trị)
            if (request.FullName != null)
                profile.FullName = request.FullName;

            if (request.AvatarUrl != null)
                profile.AvatarUrl = request.AvatarUrl;

            if (request.Bio != null)
                profile.Bio = request.Bio;

            if (request.AccessibilityNeeds != null)
                profile.AccessibilityNeeds = request.AccessibilityNeeds;

            // TravelStyle — lưu array string
            profile.TravelStyle = request.TravelStyle.Select(v => v.ToString()).ToList();

            // Generate travel_style_text bằng Gemini
            var generatedText = await GenerateTravelStyleTextAsync(request.TravelStyle, cancellationToken);
            if (!string.IsNullOrEmpty(generatedText))
                profile.TravelStyleText = generatedText;

            if (isNew)
                await _userProfileRepository.CreateAsync(profile, cancellationToken);
            else
                await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        }

        // ================================================================
        // PRIVATE
        // ================================================================

        private async Task<string?> GenerateTravelStyleTextAsync(
            List<JSEA_Application.Enums.VibeType> travelStyle,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var styleList = string.Join(", ", travelStyle.Select(v => v.ToString()));

            var prompt = $"""
            Ban la he thong du lich. Hay viet mot doan mo ta ngan (2-3 cau, tieng Viet khong dau)
            mo ta phong cach du lich cua mot nguoi co cac so thich du lich sau: {styleList}.
            Mo ta phai the hien ro rang ho thich loai dia diem nao, khong khi nhu the nao, va trai nghiem gi, nhung dia diem do phu hop voi so thich nao cua ban.
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
    }
}
