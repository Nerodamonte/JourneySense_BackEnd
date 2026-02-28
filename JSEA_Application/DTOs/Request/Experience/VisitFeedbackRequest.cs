using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Experience;

public class VisitFeedbackRequest
{
    [Required(ErrorMessage = "ExperienceId không được để trống")]
    public Guid ExperienceId { get; set; }

    [Required(ErrorMessage = "JourneyId không được để trống")]
    public Guid JourneyId { get; set; }

    /// <summary>Điểm đánh giá 1-5.</summary>
    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5")]
    public int RatingValue { get; set; }

    [StringLength(2000)]
    public string? FeedbackText { get; set; }

    public List<string>? PhotoUrls { get; set; }
}
