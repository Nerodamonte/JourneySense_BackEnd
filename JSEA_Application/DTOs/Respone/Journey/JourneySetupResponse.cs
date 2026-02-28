using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class JourneySetupResponse
{
    public Guid JourneyId { get; set; }
    public JourneyStatus Status { get; set; }
    public string Summary { get; set; } = null!;
}
