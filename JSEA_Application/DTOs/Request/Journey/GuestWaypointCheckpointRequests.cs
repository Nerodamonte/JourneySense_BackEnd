using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

public class GuestWaypointCheckInRequest
{
    [Required]
    public Guid GuestKey { get; set; }

    [StringLength(1000)]
    public string? FeedbackText { get; set; }

    public List<string>? PhotoUrls { get; set; }
}

public class GuestWaypointCheckOutRequest
{
    [Required]
    public Guid GuestKey { get; set; }

    [Range(1, 5)]
    public int? RatingValue { get; set; }
}
