using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Kết quả phân tích tuyến từ Goong Maps (distance, duration, geometry).
/// </summary>
public class RouteContext
{
    /// <summary>Geometry nội bộ của tuyến (LineString WGS84) – không serialize ra JSON.</summary>
    [JsonIgnore]
    public LineString? RoutePath { get; set; }

    /// <summary>Tổng quãng đường tuyến (mét).</summary>
    public int TotalDistanceMeters { get; set; }

    /// <summary>Thời gian di chuyển ước tính (phút) cho tuyến.</summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>Điểm xuất phát (geocode) của tuyến – nội bộ.</summary>
    [JsonIgnore]
    public Point? OriginLocation { get; set; }

    /// <summary>Điểm kết thúc (geocode) của tuyến – nội bộ.</summary>
    [JsonIgnore]
    public Point? DestinationLocation { get; set; }

    /// <summary>Vĩ độ/Kinh độ điểm xuất phát (dùng cho FE vẽ map).</summary>
    public double? OriginLatitude { get; set; }
    public double? OriginLongitude { get; set; }

    /// <summary>Vĩ độ/Kinh độ điểm đích (dùng cho FE vẽ map).</summary>
    public double? DestinationLatitude { get; set; }
    public double? DestinationLongitude { get; set; }

    /// <summary>Encoded polyline (chuỗi) của tuyến từ Goong – FE có thể decode để vẽ polyline.</summary>
    public string? Polyline { get; set; }

    /// <summary>Số lượng experiences hiện tại phù hợp dọc theo tuyến này (theo filter cứng + trạng thái).</summary>
    public int ExperienceCount { get; set; }

    /// <summary>
    /// Id của RouteSegment đã được lưu trong DB tương ứng với tuyến này.
    /// Frontend dùng để gọi POST /journeys/{journeyId}/segments/{segmentId}/suggest.
    /// Null nếu journey chưa được tạo (chỉ preview).
    /// </summary>
    public Guid? SegmentId { get; set; }
}
