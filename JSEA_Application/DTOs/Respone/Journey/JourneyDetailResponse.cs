using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Thông tin chi tiết một journey (để xem lại hoặc debug gợi ý).
/// </summary>
public class JourneyDetailResponse
{
    public Guid Id { get; set; }

    public Guid? TravelerId { get; set; }

    public string? OriginAddress { get; set; }

    public string? DestinationAddress { get; set; }

    public VehicleType? VehicleType { get; set; }

    /// <summary>Tổng quãng đường tuyến chính (mét) do Goong trả về.</summary>
    public int? TotalDistanceMeters { get; set; }

    /// <summary>Thời gian di chuyển ước tính (phút) từ Goong (không tính dừng).</summary>
    public int? EstimatedDurationMinutes { get; set; }

    /// <summary>Time budget tổng mà người dùng cấu hình (phút).</summary>
    public int? TimeBudgetMinutes { get; set; }

    /// <summary>Khoảng cách detour tối đa cho các gợi ý Experience (mét).</summary>
    public int? MaxDetourDistanceMeters { get; set; }

    /// <summary>Mood/vibe hiện tại của journey (nếu có).</summary>
    public MoodType? CurrentMood { get; set; }

    /// <summary>Trạng thái hành trình.</summary>
    public JourneyStatus? Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public string? JourneyFeedback { get; set; }

    /// <summary>Khi có nội dung feedback chuyến: pending | approved | rejected. Chủ chuyến luôn xem được nội dung; người khác chỉ khi approved.</summary>
    public string? JourneyFeedbackModerationStatus { get; set; }

    /// <summary>
    /// Main route geometry (WGS84) — route_path hiện tại (sau chọn segment).
    /// </summary>
    public List<GeoPointResponse>? RoutePoints { get; set; }

    /// <summary>Tuyến primary lúc setup (segment đầu trong route_segments), không đổi khi route_path bị ghi đè.</summary>
    public List<GeoPointResponse>? SetupPrimaryRoutePoints { get; set; }

    /// <summary>
    /// All stored route segments (alternatives) created at setup.
    /// </summary>
    public List<RouteSegmentResponse>? Segments { get; set; }

    /// <summary>
    /// Current planned stops (waypoints) in the journey.
    /// </summary>
    public List<JourneyWaypointResponse>? Waypoints { get; set; }

    /// <summary>
    /// Segment inferred from current waypoints (if any).
    /// </summary>
    public Guid? SelectedSegmentId { get; set; }
}
