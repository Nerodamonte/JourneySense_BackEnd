namespace JSEA_Infrastructure.Services.Goong;

public class GoongOptions
{
    public const string SectionName = "Goong";

    /// <summary>Tên HttpClientFactory — timeout + User-Agent, tránh một số môi trường chặn client mặc định.</summary>
    public const string HttpClientName = "Goong";

    public string ApiKey { get; set; } = string.Empty;

    public string? BaseUrl { get; set; }

    public int MaxRouteAlternatives { get; set; } = 3;
}
