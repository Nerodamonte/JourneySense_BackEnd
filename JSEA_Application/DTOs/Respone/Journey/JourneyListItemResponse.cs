using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class JourneyListItemResponse
{
    public Guid Id { get; set; }
    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public VehicleType? VehicleType { get; set; }
    public int? TimeBudgetMinutes { get; set; }
    public MoodType? CurrentMood { get; set; }
    public JourneyStatus? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}
