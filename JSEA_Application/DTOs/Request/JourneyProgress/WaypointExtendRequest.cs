using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.JourneyProgress;

public class WaypointExtendRequest
{
    [Range(1, 180)]
    public int DeltaMinutes { get; set; }
}
