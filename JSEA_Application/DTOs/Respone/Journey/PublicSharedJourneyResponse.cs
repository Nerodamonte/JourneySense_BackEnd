using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class PublicSharedJourneyResponse
{
    public string ShareCode { get; set; } = null!;

    public int ViewCount { get; set; }

    public Guid JourneyId { get; set; }

    public string? OriginAddress { get; set; }

    public string? DestinationAddress { get; set; }

    public VehicleType? VehicleType { get; set; }

    public JourneyStatus? Status { get; set; }

    public int? WaypointCount { get; set; }
}
