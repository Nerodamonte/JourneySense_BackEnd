namespace JSEA_Application.DTOs.Respone.Journey;

public class JourneyPolylineResponse
{
    public Guid JourneyId { get; set; }

    /// <summary>
    /// Nếu polyline được tính theo chế độ "đến waypoint gần nhất", đây là waypoint được chọn.
    /// Null nếu polyline là tuyến tổng (đi qua các waypoint theo StopOrder) hoặc không có waypoint.
    /// </summary>
    public Guid? TargetWaypointId { get; set; }

    /// <summary>
    /// ExperienceId tương ứng với TargetWaypointId (nếu có).
    /// </summary>
    public Guid? TargetExperienceId { get; set; }

    /// <summary>
    /// Encoded polyline string (Goong/Google format). FE có thể decode để vẽ.
    /// Có thể null nếu backend không lấy được encoded polyline.
    /// </summary>
    public string? Polyline { get; set; }

    /// <summary>
    /// Danh sách điểm (lat/lng) để FE vẽ polyline trực tiếp.
    /// Luôn cố gắng trả về (dựa trên decode polyline hoặc RoutePath trong DB).
    /// </summary>
    public List<GeoPointResponse> Points { get; set; } = new();

    public int? DistanceMeters { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
}
