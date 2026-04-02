namespace JSEA_Application.DTOs.Journey;

/// <summary>SignalR method <c>EmergencyPlaceSelected</c> (camelCase JSON).</summary>
public class JourneyEmergencySelectionNotification
{
    public Guid JourneyId { get; set; }
    /// <summary><see cref="Guid.Empty"/> nếu người bấm khẩn cấp là khách.</summary>
    public Guid AnnouncedByTravelerId { get; set; }
    /// <summary>Chỉ set khi khách; client có thể dùng cùng <c>AnnouncedByDisplayName</c>.</summary>
    public Guid? AnnouncedByGuestKey { get; set; }
    public string AnnouncedByDisplayName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string PlaceId { get; set; } = null!;
    public string? Name { get; set; }
    public string? FormattedAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime AtUtc { get; set; }
}
