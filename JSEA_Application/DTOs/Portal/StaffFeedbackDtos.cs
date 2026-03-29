using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Portal;

public class StaffFeedbackListItemDto
{
    public Guid Id { get; set; }
    public string FeedbackText { get; set; } = null!;
    public string ModerationStatus { get; set; } = null!;
    public bool? IsFlagged { get; set; }
    public string? FlaggedReason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public Guid ExperienceId { get; set; }
    public string? ExperienceName { get; set; }
    public Guid TravelerId { get; set; }
    public string? TravelerEmail { get; set; }
}

public class StaffFeedbackDetailDto : StaffFeedbackListItemDto
{
    /// <summary>Nếu feedback đến từ check-in trên chuyến đi.</summary>
    public Guid? JourneyId { get; set; }

    /// <summary>Feedback chung cả chuyến (trường journeys.journey_feedback).</summary>
    public string? JourneyFeedback { get; set; }

    /// <summary>Trạng thái duyệt feedback cả chuyến (journeys).</summary>
    public string? JourneyFeedbackModerationStatus { get; set; }

    /// <summary>Thứ tự dừng waypoint tương ứng experience (nếu có).</summary>
    public int? WaypointStopOrder { get; set; }
}

public class StaffJourneyFeedbackListItemDto
{
    public Guid JourneyId { get; set; }
    public Guid TravelerId { get; set; }
    public string? TravelerEmail { get; set; }
    public string JourneyFeedback { get; set; } = null!;
    public string ModerationStatus { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
}

public class ModerateFeedbackRequest
{
    /// <summary>approve | reject</summary>
    [Required]
    public string Decision { get; set; } = null!;

    public string? Reason { get; set; }
   
}

public class ReportPortalUserRequest
{
    [Required]
    public string Reason { get; set; } = null!;

    public Guid? RelatedFeedbackId { get; set; }
}
