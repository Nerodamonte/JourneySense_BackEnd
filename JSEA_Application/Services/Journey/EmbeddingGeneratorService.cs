using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.Extensions.Configuration;
using Pgvector;
using System.Text;
using System.Text.Json;

namespace JSEA_Application.Services.Journey;

public class EmbeddingGeneratorService
{
    private readonly IMicroExperienceRepository _experienceRepository;
    private readonly IExperienceEmbeddingRepository _embeddingRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string GeminiEmbedModel = "gemini-embedding-001";

    public EmbeddingGeneratorService(
        IMicroExperienceRepository experienceRepository,
        IExperienceEmbeddingRepository embeddingRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _experienceRepository = experienceRepository;
        _embeddingRepository = embeddingRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Generate embeddings cho tất cả experiences active chưa có embedding.
    /// Trả về (success, failed, errors).
    /// </summary>
    public async Task<(int success, int failed, List<string> errors)> GenerateForAllAsync(
        CancellationToken cancellationToken = default)
    {
        var experiences = await _experienceRepository.GetActiveWithoutEmbeddingAsync(cancellationToken);

        int success = 0, failed = 0;
        var errors = new List<string>();

        foreach (var exp in experiences)
        {
            try
            {
                var metadata = BuildMetadataString(exp);
                var vector = await EmbedTextAsync(metadata, cancellationToken);

                if (vector == null)
                {
                    failed++;
                    errors.Add($"{exp.Name}: Gemini API trả về null");
                    continue;
                }

                await _embeddingRepository.UpsertAsync(exp.Id, metadata, vector, cancellationToken);
                success++;

                // Delay nhỏ để tránh rate limit Gemini
                await Task.Delay(200, cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{exp.Name}: {ex.Message}");
            }
        }

        return (success, failed, errors);
    }

    /// <summary>
    /// Build metadata string cho một experience.
    /// Phải cùng format/ngôn ngữ với BuildUserMetadataString trong SuggestService
    /// để cosine similarity có ý nghĩa khi so sánh.
    /// </summary>
    private static string BuildMetadataString(Models.Experience exp)
    {
        var parts = new List<string>();

        parts.Add(exp.Name);

        if (exp.Category?.Name != null)
            parts.Add(exp.Category.Name);

        if (!string.IsNullOrEmpty(exp.ExperienceDetail?.RichDescription))
            parts.Add(exp.ExperienceDetail.RichDescription);

        if (!string.IsNullOrEmpty(exp.ExperienceDetail?.PriceRange))
            parts.Add($"Gia: {exp.ExperienceDetail.PriceRange}");

        if (!string.IsNullOrEmpty(exp.ExperienceDetail?.CrowdLevel))
            parts.Add($"Dong duc: {exp.ExperienceDetail.CrowdLevel}");

        if (exp.PreferredTimes?.Count > 0)
            parts.Add($"Thoi diem: {string.Join(", ", exp.PreferredTimes)}");

        if (exp.AccessibleBy?.Count > 0)
            parts.Add($"Phuong tien: {string.Join(", ", exp.AccessibleBy)}");

        if (exp.Tags?.Count > 0)
            parts.Add($"Tags: {string.Join(", ", exp.Tags)}");

        if (exp.AmenityTags?.Count > 0)
            parts.Add($"Tien ich: {string.Join(", ", exp.AmenityTags)}");

        return string.Join(". ", parts);
    }

    /// <summary>Sinh lại embedding cho một experience (sau khi staff cập nhật nội dung).</summary>
    public async Task<(bool Ok, string? Error)> RegenerateEmbeddingForExperienceAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        var exp = await _experienceRepository.GetByIdAsync(experienceId, cancellationToken);
        if (exp == null)
            return (false, "Không tìm thấy địa điểm.");
        if (exp.Status != "active")
            return (false, "Địa điểm không active, bỏ qua embedding.");

        var metadata = BuildMetadataString(exp);
        var vector = await EmbedTextAsync(metadata, cancellationToken);
        if (vector == null)
            return (false, "Gemini embedding trả về null.");

        await _embeddingRepository.UpsertAsync(exp.Id, metadata, vector, cancellationToken);
        return (true, null);
    }

    private async Task<Vector?> EmbedTextAsync(string text, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        var client = _httpClientFactory.CreateClient();

        var url =
   $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiEmbedModel}:embedContent?key={apiKey}";

        var body = JsonSerializer.Serialize(new
        {
            model = $"models/{GeminiEmbedModel}",
            content = new
            {
                parts = new[]
                {
                new { text }
            }
            }
        });

        var response = await client.PostAsync(
            url,
            new StringContent(body, Encoding.UTF8, "application/json"),
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        Console.WriteLine("Gemini Response:");
        Console.WriteLine(json);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Gemini Error: {response.StatusCode}");
            return null;
        }



        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("embedding", out var embedding))
            return null;

        var values = embedding.GetProperty("values");

        var floats = values
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();

        return new Vector(floats);
    }
}