namespace JSEA_Application.DTOs.Journey;

/// <summary>SignalR: <c>JourneyStarted</c> — owner vừa bấm start lần đầu.</summary>
public class JourneyStartedLiveNotification
{
    public Guid JourneyId { get; set; }

    public DateTime StartedAt { get; set; }
}
