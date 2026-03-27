namespace JSEA_Infrastructure.Services.OpenMeteo;

/// <summary>Cấu hình TTL cache thời tiết (Redis / memory).</summary>
public class WeatherCacheOptions
{
    public const string SectionName = "WeatherCache";

    /// <summary>Thời gian sống của mỗi key cache (phút). Mặc định 10 phút — phù hợp dữ liệu “current weather”.</summary>
    public int AbsoluteExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// <c>false</c>: luôn dùng cache trong RAM (không kết nối Redis) — phù hợp máy dev chưa chạy Redis.
    /// <c>null</c> hoặc <c>true</c>: nếu có <c>ConnectionStrings:Redis</c> thì dùng Redis.
    /// </summary>
    public bool? UseRedis { get; set; }
}
