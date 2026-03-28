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
