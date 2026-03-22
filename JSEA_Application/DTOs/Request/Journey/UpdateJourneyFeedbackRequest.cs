using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

public class UpdateJourneyFeedbackRequest
{
    [StringLength(8000)]
    public string? JourneyFeedback { get; set; }
}
