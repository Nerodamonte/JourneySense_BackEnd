using System.Linq;
using System.Text;
using System.Text.Json;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JSEA_Application.Services.Profile;

public class TravelStyleTextGenerator : ITravelStyleTextGenerator
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string GeminiGenerateModel = "gemini-2.5-flash";

    public TravelStyleTextGenerator(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GenerateAsync(IReadOnlyList<VibeType> travelStyle, CancellationToken cancellationToken = default)
    {
        if (travelStyle == null || travelStyle.Count == 0)
            return null;

        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return null;

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

        var response = await client.PostAsync(
            url,
            new StringContent(body, Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

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
