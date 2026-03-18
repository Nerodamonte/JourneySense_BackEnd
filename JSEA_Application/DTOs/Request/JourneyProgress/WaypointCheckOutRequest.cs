using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.JourneyProgress;

public class WaypointCheckOutRequest
{
    [Range(1, 5)]
    public int RatingValue { get; set; }
}
