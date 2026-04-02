using JSEA_Application.Constants;
using JSEA_Application.DTOs.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace JSEA_Presentation.Services;

public class JourneyLiveNotifier : IJourneyLiveNotifier
{
    private readonly IHubContext<JourneyLiveHub> _hub;
    private readonly ILogger<JourneyLiveNotifier> _logger;

    public JourneyLiveNotifier(IHubContext<JourneyLiveHub> hub, ILogger<JourneyLiveNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyDestinationMemberArrivedAsync(
        JourneyDestinationArrivedNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(JourneyLiveGroups.ForJourney(notification.JourneyId))
                .SendAsync("DestinationMemberArrived", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SignalR DestinationMemberArrived failed for journey {JourneyId}",
                notification.JourneyId);
        }
    }

    public async Task NotifyEmergencyPlaceSelectedAsync(
        JourneyEmergencySelectionNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(JourneyLiveGroups.ForJourney(notification.JourneyId))
                .SendAsync("EmergencyPlaceSelected", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SignalR EmergencyPlaceSelected failed for journey {JourneyId}",
                notification.JourneyId);
        }
    }

    public async Task NotifyMemberLocationAsync(
        JourneyMemberLocationNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(JourneyLiveGroups.ForJourney(notification.JourneyId))
                .SendAsync("MemberLocationUpdated", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SignalR MemberLocationUpdated failed for journey {JourneyId}",
                notification.JourneyId);
        }
    }

    public async Task NotifyWaypointAttendanceUpdatedAsync(
        JourneyWaypointAttendanceResponse snapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(JourneyLiveGroups.ForJourney(snapshot.JourneyId))
                .SendAsync("WaypointAttendanceUpdated", snapshot, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SignalR WaypointAttendanceUpdated failed for journey {JourneyId}",
                snapshot.JourneyId);
        }
    }
}
