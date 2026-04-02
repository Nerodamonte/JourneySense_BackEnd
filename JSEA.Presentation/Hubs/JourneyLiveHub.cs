using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Presentation.Services;
using Microsoft.AspNetCore.SignalR;

namespace JSEA_Presentation.Hubs;

/// <summary>
/// Realtime chuyến đi: <c>JoinJourney</c> sau khi kết nối; <c>UpdateLocation</c> gửi GPS qua WebSocket (fallback nếu REST không ổn).
/// Cả hai đều ghi Redis + broadcast SignalR.
/// </summary>
public class JourneyLiveHub : Hub
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JourneyLiveLocationRateLimiter _locationRateLimiter;
    private readonly IJourneyLiveNotifier _journeyLiveNotifier;
    private readonly IJourneyLocationCache _locationCache;

    public JourneyLiveHub(
        IServiceProvider serviceProvider,
        JourneyLiveLocationRateLimiter locationRateLimiter,
        IJourneyLiveNotifier journeyLiveNotifier,
        IJourneyLocationCache locationCache)
    {
        _serviceProvider = serviceProvider;
        _locationRateLimiter = locationRateLimiter;
        _journeyLiveNotifier = journeyLiveNotifier;
        _locationCache = locationCache;
    }

    /// <param name="journeyId">Id hành trình.</param>
    /// <param name="guestKey">Bắt buộc nếu connection không có JWT; user đăng nhập có thể null.</param>
    public async Task JoinJourney(Guid journeyId, Guid? guestKey = null)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var journeys = scope.ServiceProvider.GetRequiredService<IJourneyRepository>();
        var memberRepo = scope.ServiceProvider.GetRequiredService<IJourneyMemberRepository>();

        var journey = await journeys.GetBasicByIdAsync(journeyId, Context.ConnectionAborted);
        if (journey == null)
            throw new HubException("Không tìm thấy hành trình.");
        if (!journey.StartedAt.HasValue)
            throw new HubException("Hành trình chưa bắt đầu.");

        Guid? travelerId = null;
        if (Context.User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (travelerId.HasValue)
        {
            if (journey.TravelerId == travelerId.Value)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, JourneyLiveGroups.ForJourney(journeyId));
                return;
            }

            var m = await memberRepo.GetActiveByTravelerAsync(journeyId, travelerId.Value, Context.ConnectionAborted);
            if (m != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, JourneyLiveGroups.ForJourney(journeyId));
                return;
            }

            throw new HubException("Bạn chưa tham gia hành trình này.");
        }

        if (guestKey.HasValue)
        {
            var g = await memberRepo.GetActiveByGuestKeyAsync(journeyId, guestKey.Value, Context.ConnectionAborted);
            if (g != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, JourneyLiveGroups.ForJourney(journeyId));
                return;
            }
        }

        throw new HubException("Chưa xác thực (đăng nhập hoặc guestKey).");
    }

    /// <summary>
    /// GPS qua WebSocket (fallback khi REST không ổn). Cùng pipeline: validate → Redis → SignalR broadcast. Rate limit ~750ms.
    /// </summary>
    public async Task UpdateLocation(
        Guid journeyId,
        double latitude,
        double longitude,
        Guid? guestKey = null,
        double? accuracyMeters = null,
        double? headingDegrees = null)
    {
        if (!IsValidGps(latitude, longitude))
            throw new HubException("GPS không hợp lệ (kiểm tra latitude/longitude).");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var journeys = scope.ServiceProvider.GetRequiredService<IJourneyRepository>();
        var memberRepo = scope.ServiceProvider.GetRequiredService<IJourneyMemberRepository>();

        var journey = await journeys.GetBasicByIdAsync(journeyId, Context.ConnectionAborted);
        if (journey == null)
            throw new HubException("Không tìm thấy hành trình.");
        if (!journey.StartedAt.HasValue)
            throw new HubException("Hành trình chưa bắt đầu.");

        Guid? travelerId = null;
        if (Context.User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        JourneyMember? member = null;
        if (travelerId.HasValue)
            member = await memberRepo.GetActiveByTravelerAsync(journeyId, travelerId.Value, Context.ConnectionAborted);
        else if (guestKey.HasValue)
            member = await memberRepo.GetActiveByGuestKeyAsync(journeyId, guestKey.Value, Context.ConnectionAborted);

        if (member == null)
            throw new HubException("Bạn chưa tham gia hành trình này.");

        if (!_locationRateLimiter.TryAllow(member.Id))
            return;

        var notification = new JourneyMemberLocationNotification
        {
            JourneyId = journeyId,
            MemberId = member.Id,
            TravelerId = member.TravelerId,
            GuestKey = member.GuestKey,
            DisplayName = member.DisplayName,
            Role = member.Role,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            HeadingDegrees = headingDegrees,
            AtUtc = DateTime.UtcNow
        };

        await _locationCache.SetAsync(notification, Context.ConnectionAborted);
        await _journeyLiveNotifier.NotifyMemberLocationAsync(notification, Context.ConnectionAborted);
    }

    public async Task LeaveJourney(Guid journeyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, JourneyLiveGroups.ForJourney(journeyId));
    }

    private static bool IsValidGps(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude) ||
            double.IsNaN(longitude) || double.IsInfinity(longitude))
            return false;
        if (Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            return false;
        if (Math.Abs(latitude) < 1e-9 && Math.Abs(longitude) < 1e-9)
            return false;
        return true;
    }
}
