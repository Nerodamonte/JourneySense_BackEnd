namespace JSEA_Infrastructure.Services.Goong;

public class GoongOptions
{
    public const string SectionName = "Goong";

    /// <summary>REST API Key (bắt buộc).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Phiên bản API: "v1" (mặc định, không path) hoặc "v2". Hiện Goong chỉ hỗ trợ base không /v2/.</summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>Base URL tùy chỉnh. Nếu set sẽ bỏ qua ApiVersion. Ví dụ: https://rsapi.goong.io/v2</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Số tuyến đường tối đa lấy từ Goong Direction (nếu API trả về nhiều routes). Mặc định 1.</summary>
    public int MaxRouteAlternatives { get; set; } = 1;
}
