namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>Một địa điểm từ Goong Place Detail + quãng đường Direction (nếu có).</summary>
public class EmergencyNearbyItemResponse
{
    public string PlaceId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Name { get; set; }
    public string? FormattedAddress { get; set; }
    public string? Phone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// Bình thường: quãng đường theo Goong Direction (m). Nếu Direction lỗi: đường chim bay (haversine), làm tròn mét.
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>Khi false: <see cref="DistanceMeters"/> là lái xe; khi true: fallback chim bay vì Goong Direction không trả tuyến.</summary>
    public bool UsedStraightLineFallback { get; set; }

    /// <summary>
    /// Polyline mã hoá (overview) từ Goong Direction — cùng kiểu Google encoded polyline. FE decode (Goong Maps / Google Maps SDK) để vẽ đường từ GPS user tới điểm đến.
    /// Null khi <see cref="UsedStraightLineFallback"/> (vẽ đoạn thẳng giữa origin đã gửi và <see cref="Latitude"/>/<see cref="Longitude"/> hoặc gọi Direction phía client).
    /// </summary>
    public string? RoutePolyline { get; set; }

    /// <summary>Phút lái xe (chỉ khi có Direction).</summary>
    public int? EstimatedDurationMinutes { get; set; }

    public double? Rating { get; set; }
    public int? UserRatingsTotal { get; set; }
    public string? OpeningHoursSummary { get; set; }
    public bool? OpenNow { get; set; }
}
