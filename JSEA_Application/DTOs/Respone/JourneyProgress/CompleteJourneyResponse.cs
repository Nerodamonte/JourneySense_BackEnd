namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class CompleteJourneyResponse
{
    public Guid JourneyId { get; set; }
    public DateTime CompletedAt { get; set; }
}
