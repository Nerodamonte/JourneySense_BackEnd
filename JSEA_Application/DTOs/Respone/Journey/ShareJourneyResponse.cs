namespace JSEA_Application.DTOs.Respone.Journey;

public class ShareJourneyResponse
{
    public string ShareCode { get; set; } = null!;

    /// <summary>Đường dẫn API xem preview (public).</summary>
    public string SharePath { get; set; } = null!;

    /// <summary>
    /// Link mở app (web/deep link) để vào màn join; null nếu chưa cấu hình PublicAppBaseUrl.
    /// Client vẫn phải gọi POST join với JWT / join-guest — link chỉ mở đúng route.
    /// </summary>
    public string? ShareLink { get; set; }

    public int PointsEarned { get; set; }
}
