namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class WaypointSkipResponse
{
    public Guid JourneyId { get; set; }
    public Guid WaypointId { get; set; }

    public DateTime? ActualArrivalAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public int? ActualStopMinutes { get; set; }
}
