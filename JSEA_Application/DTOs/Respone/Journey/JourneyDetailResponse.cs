using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class JourneyDetailResponse
{
    public Guid Id { get; set; }
    public Guid? TravelerId { get; set; }
    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public VehicleType? VehicleType { get; set; }
    public int? TotalDistanceMeters { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int? TimeBudgetMinutes { get; set; }
    public int? MaxDetourDistanceMeters { get; set; }
    public MoodType? CurrentMood { get; set; }
    public int? PreferredStopDurationMinutes { get; set; }
    public JourneyStatus? Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
