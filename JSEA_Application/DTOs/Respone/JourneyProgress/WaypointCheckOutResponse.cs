namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class WaypointCheckOutResponse
{
    public Guid JourneyId { get; set; }
    public Guid WaypointId { get; set; }

    public Guid VisitId { get; set; }
    public Guid? RatingId { get; set; }

    public DateTime? ActualDepartureAt { get; set; }
    public int? ActualStopMinutes { get; set; }
}
