using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.JourneyProgress;

public class WaypointCheckInRequest
{
    [StringLength(1000)]
    public string? FeedbackText { get; set; }

    public List<string>? PhotoUrls { get; set; }
}
