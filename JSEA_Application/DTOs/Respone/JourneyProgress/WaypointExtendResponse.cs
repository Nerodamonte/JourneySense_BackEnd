namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class WaypointExtendResponse
{
    public Guid JourneyId { get; set; }
    public Guid WaypointId { get; set; }

    public int? PlannedStopMinutes { get; set; }
}
