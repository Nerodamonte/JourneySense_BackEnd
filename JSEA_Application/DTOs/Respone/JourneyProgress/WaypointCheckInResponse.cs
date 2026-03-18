namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class WaypointCheckInResponse
{
    public Guid JourneyId { get; set; }
    public Guid WaypointId { get; set; }

    public Guid VisitId { get; set; }
    public Guid? FeedbackId { get; set; }

    public DateTime VisitedAt { get; set; }
    public DateTime? ActualArrivalAt { get; set; }
}
