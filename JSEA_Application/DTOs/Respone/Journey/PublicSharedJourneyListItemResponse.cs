using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class PublicSharedJourneyListItemResponse
{
    public string ShareCode { get; set; } = null!;

    public Guid JourneyId { get; set; }

    public string? TravelerName { get; set; }
    public string? TravelerAvatarUrl { get; set; }

    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }

    public VehicleType? VehicleType { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int ViewCount { get; set; }

    public int WaypointCount { get; set; }
}
