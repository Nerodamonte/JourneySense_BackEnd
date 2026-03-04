using NetTopologySuite.Geometries;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Kết quả phân tích tuyến từ Goong Maps (distance, duration, geometry).
/// </summary>
public class RouteContext
{
    /// <summary>Geometry của tuyến (LineString WGS84) để vẽ trên bản đồ.</summary>
    public LineString? RoutePath { get; set; }

    /// <summary>Tổng quãng đường tuyến (mét).</summary>
    public int TotalDistanceMeters { get; set; }

    /// <summary>Thời gian di chuyển ước tính (phút) cho tuyến.</summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>Điểm xuất phát (geocode) của tuyến.</summary>
    public Point? OriginLocation { get; set; }

    /// <summary>Điểm kết thúc (geocode) của tuyến.</summary>
    public Point? DestinationLocation { get; set; }

    /// <summary>Số lượng experiences hiện tại phù hợp dọc theo tuyến này (theo filter cứng + trạng thái).</summary>
    public int ExperienceCount { get; set; }
}
