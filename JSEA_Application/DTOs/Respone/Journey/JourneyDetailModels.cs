namespace JSEA_Application.DTOs.Respone.Journey;

public class GeoPointResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class RouteSegmentResponse
{
    public Guid SegmentId { get; set; }
    public int? SegmentOrder { get; set; }
    public int? DistanceMeters { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool? IsScenic { get; set; }
    public bool? IsBusy { get; set; }
    public bool? IsCulturalArea { get; set; }
}

/// <summary>Feedback + rating gắn với lần check-in (visit) tại waypoint — lồng trong chi tiết journey.</summary>
public class JourneyWaypointVisitFeedbackResponse
{
    public Guid VisitId { get; set; }
    public Guid? FeedbackId { get; set; }
    public string? FeedbackText { get; set; }
    public string? ModerationStatus { get; set; }
    public DateTime? FeedbackCreatedAt { get; set; }
    /// <summary>Điểm đánh giá 1–5 (nếu có) cùng visit.</summary>
    public int? Rating { get; set; }
}

public class JourneyWaypointResponse
{
    public Guid WaypointId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? SuggestionId { get; set; }
    public Guid? SegmentId { get; set; }

    public int StopOrder { get; set; }

    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public int? DetourDistanceMeters { get; set; }
    public int? DetourTimeMinutes { get; set; }

    /// <summary>Thông tin check-in + feedback waypoint (null nếu chưa ghé).</summary>
    public JourneyWaypointVisitFeedbackResponse? VisitFeedback { get; set; }
}
