using NetTopologySuite.Geometries;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Kết quả phân tích tuyến từ Goong Maps (distance, duration, geometry).
/// </summary>
public class RouteContext
{
    public LineString? RoutePath { get; set; }
    public int TotalDistanceMeters { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public Point? OriginLocation { get; set; }
    public Point? DestinationLocation { get; set; }
}
