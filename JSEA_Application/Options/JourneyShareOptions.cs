namespace JSEA_Application.Options;

/// <summary>
/// URL app (web/mobile) để mở màn join; client đọc share code từ path/query rồi gọi POST join.
/// </summary>
public class JourneyShareOptions
{
    public const string SectionName = "JourneyShare";

    /// <summary>VD: https://app.example.com hoặc journeysense:// (deep link)</summary>
    public string PublicAppBaseUrl { get; set; } = "";

    /// <summary>Format cho segment sau base; {0} = shareCode. Mặc định /join/ABC.</summary>
    public string JoinPathFormat { get; set; } = "/join/{0}";
}
