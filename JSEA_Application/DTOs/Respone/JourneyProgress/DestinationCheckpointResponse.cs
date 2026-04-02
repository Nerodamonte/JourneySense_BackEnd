namespace JSEA_Application.DTOs.Respone.JourneyProgress;

public class DestinationCheckpointResponse
{
    public Guid JourneyId { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? DepartedAt { get; set; }
}
