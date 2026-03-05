namespace JSEA_Infrastructure.Services.Goong;

public class GoongOptions
{
    public const string SectionName = "Goong";

    public string ApiKey { get; set; } = string.Empty;

    public string? BaseUrl { get; set; }

    public int MaxRouteAlternatives { get; set; } = 3;
}
