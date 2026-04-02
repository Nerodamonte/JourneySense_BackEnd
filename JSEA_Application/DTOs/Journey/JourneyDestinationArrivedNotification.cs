namespace JSEA_Application.DTOs.Journey;

/// <summary>
/// Payload SignalR method <c>DestinationMemberArrived</c> (camelCase JSON).
/// </summary>
public class JourneyDestinationArrivedNotification
{
    public Guid JourneyId { get; set; }
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsGuest { get; set; }
    public DateTime ArrivedAt { get; set; }
}
