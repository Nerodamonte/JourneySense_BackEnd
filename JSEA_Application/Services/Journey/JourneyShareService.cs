using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Application.Options;
using JourneyEntity = JSEA_Application.Models.Journey;
using Microsoft.Extensions.Options;

namespace JSEA_Application.Services.Journey;

public class JourneyShareService : IJourneyShareService
{
    public const string ShareAchievementCode = "SHARE_JOURNEY";
    private const int ShareJourneyPoints = 10;

    private readonly IJourneyRepository _journeyRepository;
    private readonly ISharedJourneyRepository _sharedJourneyRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IRewardService _rewardService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IGoongMapsService _goongMapsService;
    private readonly IJourneyMemberRepository _journeyMemberRepository;
    private readonly JourneyShareOptions _journeyShareOptions;

    /// <summary>Đủ điểm trên LineString đã lưu → không cần gọi lại Directions.</summary>
    private const int MinStoredRouteVerticesForShare = 4;

    public JourneyShareService(
        IJourneyRepository journeyRepository,
        ISharedJourneyRepository sharedJourneyRepository,
        IAchievementRepository achievementRepository,
        IRewardService rewardService,
        IUserProfileRepository userProfileRepository,
        IVisitRepository visitRepository,
        IFeedbackRepository feedbackRepository,
        IRatingRepository ratingRepository,
        IGoongMapsService goongMapsService,
        IJourneyMemberRepository journeyMemberRepository,
        IOptions<JourneyShareOptions> journeyShareOptions)
    {
        _journeyRepository = journeyRepository;
        _sharedJourneyRepository = sharedJourneyRepository;
        _achievementRepository = achievementRepository;
        _rewardService = rewardService;
        _userProfileRepository = userProfileRepository;
        _visitRepository = visitRepository;
        _feedbackRepository = feedbackRepository;
        _ratingRepository = ratingRepository;
        _goongMapsService = goongMapsService;
        _journeyMemberRepository = journeyMemberRepository;
        _journeyShareOptions = journeyShareOptions?.Value ?? new JourneyShareOptions();
    }

    public async Task<ShareJourneyResponse?> ShareJourneyAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null || journey.TravelerId != travelerId)
            return null;

        var existing = await _sharedJourneyRepository.GetActiveByJourneyAndUserAsync(
            journeyId,
            travelerId,
            cancellationToken);

        if (existing != null)
        {
            return new ShareJourneyResponse
            {
                ShareCode = existing.ShareCode,
                SharePath = $"/api/journeys/shared/{existing.ShareCode}",
                ShareLink = BuildClientJoinLink(existing.ShareCode),
                PointsEarned = 0
            };
        }

        var shareCode = await GenerateUniqueShareCodeAsync(cancellationToken);
        var row = new SharedJourney
        {
            JourneyId = journeyId,
            UserId = travelerId,
            ShareCode = shareCode,
            ViewCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _sharedJourneyRepository.AddAsync(row, cancellationToken);

        var points = ShareJourneyPoints;
        await _rewardService.AddRewardPointsAsync(
            travelerId,
            points,
            "share_journey",
            cancellationToken,
            achievementId: null,
            refId: journeyId,
            refType: "journey");

        return new ShareJourneyResponse
        {
            ShareCode = shareCode,
            SharePath = $"/api/journeys/shared/{shareCode}",
            ShareLink = BuildClientJoinLink(shareCode),
            PointsEarned = points
        };
    }

    /// <summary>Link mở frontend / app; join thật vẫn qua API (cần token hoặc guest body).</summary>
    private string? BuildClientJoinLink(string shareCode)
    {
        var baseUrl = _journeyShareOptions.PublicAppBaseUrl?.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return null;

        var fmt = string.IsNullOrWhiteSpace(_journeyShareOptions.JoinPathFormat)
            ? "/join/{0}"
            : _journeyShareOptions.JoinPathFormat.Trim();

        string path;
        try
        {
            path = string.Format(fmt, Uri.EscapeDataString(shareCode));
        }
        catch (FormatException)
        {
            path = $"/join/{Uri.EscapeDataString(shareCode)}";
        }

        if (!path.StartsWith('/'))
            path = '/' + path;

        return baseUrl.TrimEnd('/') + path;
    }

    public async Task<PublicSharedJourneyResponse?> GetPublicByShareCodeAsync(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            return null;

        var normalized = shareCode.Trim();
        var row = await _sharedJourneyRepository.GetByShareCodeWithJourneyAsync(normalized, cancellationToken);
        if (row?.Journey == null)
            return null;

        row.ViewCount++;
        await _sharedJourneyRepository.UpdateAsync(row, cancellationToken);

        var j = row.Journey;
        return new PublicSharedJourneyResponse
        {
            ShareCode = row.ShareCode,
            ViewCount = row.ViewCount,
            JourneyId = j.Id,
            OriginAddress = j.OriginAddress,
            DestinationAddress = j.DestinationAddress,
            VehicleType = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt) ? vt : null,
            Status = Enum.TryParse<JourneyStatus>(j.Status, true, out var js) ? js : null,
            WaypointCount = j.JourneyWaypoints?.Count
        };
    }

    public async Task<List<PublicSharedJourneyListItemResponse>> GetPublicSharedJourneysAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var rows = await _sharedJourneyRepository.GetPublicCompletedAsync(page, pageSize, cancellationToken);

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var profiles = new Dictionary<Guid, UserProfile>();
        foreach (var uid in userIds)
        {
            var p = await _userProfileRepository.GetByUserIdAsync(uid, cancellationToken);
            if (p != null)
                profiles[uid] = p;
        }

        return rows
            .Where(r => r.Journey != null)
            .Select(r =>
            {
                profiles.TryGetValue(r.UserId, out var p);
                var j = r.Journey;
                return new PublicSharedJourneyListItemResponse
                {
                    ShareCode = r.ShareCode,
                    JourneyId = j.Id,
                    TravelerName = p?.FullName,
                    TravelerAvatarUrl = p?.AvatarUrl,
                    OriginAddress = j.OriginAddress,
                    DestinationAddress = j.DestinationAddress,
                    VehicleType = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt) ? vt : null,
                    CompletedAt = j.CompletedAt,
                    ViewCount = r.ViewCount,
                    WaypointCount = j.JourneyWaypoints?.Count ?? 0
                };
            })
            .ToList();
    }

    public async Task<PublicSharedJourneyDetailResponse?> GetPublicDetailByShareCodeAsync(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            return null;

        var normalized = shareCode.Trim();
        var row = await _sharedJourneyRepository.GetPublicDetailAsync(normalized, cancellationToken);
        if (row?.Journey == null)
            return null;

        row.ViewCount++;
        await _sharedJourneyRepository.UpdateAsync(row, cancellationToken);

        var profile = await _userProfileRepository.GetByUserIdAsync(row.UserId, cancellationToken);

        var j = row.Journey;
        var waypoints = (j.JourneyWaypoints ?? new List<JourneyWaypoint>())
            .OrderBy(w => w.StopOrder)
            .ToList();

        var visits = await _visitRepository.GetByJourneyTravelerAsync(j.Id, j.TravelerId, cancellationToken);
        var visitByExperienceId = visits
            .GroupBy(v => v.ExperienceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.VisitedAt).First());

        var waypointResponses = new List<PublicSharedJourneyWaypointResponse>();
        foreach (var w in waypoints)
        {
            visitByExperienceId.TryGetValue(w.ExperienceId, out var visit);
            var feedback = visit?.Feedback;
            var rating = visit?.Rating;
            var waypointFbApproved = feedback == null || string.Equals(
                feedback.ModerationStatus,
                FeedbackModerationStatuses.Approved,
                StringComparison.OrdinalIgnoreCase);

            waypointResponses.Add(new PublicSharedJourneyWaypointResponse
            {
                WaypointId = w.Id,
                StopOrder = w.StopOrder,
                ExperienceId = w.ExperienceId,
                ExperienceName = w.Experience?.Name,
                ExperienceAddress = w.Experience?.Address,
                ExperienceLatitude = w.Experience?.Location?.Y,
                ExperienceLongitude = w.Experience?.Location?.X,
                ExperienceDescription = w.Experience?.ExperienceDetail?.RichDescription,
                ExperiencePhotoUrls = w.Experience?.ExperiencePhotos
                    ?.OrderByDescending(p => p.IsCover == true)
                    .ThenByDescending(p => p.UploadedAt)
                    .Select(p => p.PhotoUrl)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Take(10)
                    .ToList() ?? new List<string>(),
                ActualArrivalAt = w.ActualArrivalAt,
                ActualDepartureAt = w.ActualDepartureAt,
                RatingValue = rating?.Rating1,
                FeedbackText = waypointFbApproved ? feedback?.FeedbackText : null,
                FeedbackCreatedAt = waypointFbApproved ? feedback?.CreatedAt : null
            });
        }

        var journeyFbPublic = string.Equals(
            j.JourneyFeedbackModerationStatus,
            FeedbackModerationStatuses.Approved,
            StringComparison.OrdinalIgnoreCase);

        var routePoints = await ResolveShareDisplayRouteAsync(j, cancellationToken);

        return new PublicSharedJourneyDetailResponse
        {
            ShareCode = row.ShareCode,
            JourneyId = j.Id,
            TravelerName = profile?.FullName,
            TravelerAvatarUrl = profile?.AvatarUrl,
            OriginAddress = j.OriginAddress,
            DestinationAddress = j.DestinationAddress,
            VehicleType = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt) ? vt : null,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt,
            ViewCount = row.ViewCount,
            JourneyFeedback = journeyFbPublic ? j.JourneyFeedback : null,
            RoutePoints = routePoints,
            SetupPrimaryRoutePoints = JourneyRoutePointsHelper.SetupPrimaryRouteFromSegments(j),
            Waypoints = waypointResponses
        };
    }

    /// <summary>
    /// Ưu tiên polyline đã lưu trên journey; nếu thiếu/không đủ chi tiết thì gọi Goong Directions origin→destination để FE vẽ đúng tuyến.
    /// </summary>
    private async Task<List<GeoPointResponse>?> ResolveShareDisplayRouteAsync(
        JourneyEntity j,
        CancellationToken cancellationToken)
    {
        var storedCount = j.RoutePath?.Coordinates?.Length ?? 0;
        if (storedCount >= MinStoredRouteVerticesForShare)
            return JourneyRoutePointsHelper.FromJourney(j);

        if (j.OriginLocation != null && j.DestinationLocation != null)
        {
            var vehicle = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt)
                ? vt
                : VehicleType.Motorbike;
            var directed = await GoongDirectionVehicleFallback.GetDirectionFirstSuccessfulAsync(
                _goongMapsService,
                j.OriginLocation,
                j.DestinationLocation,
                vehicle,
                waypoints: null,
                cancellationToken);
            var fromGoong = JourneyRoutePointsHelper.FromRouteContext(directed);
            if (fromGoong is { Count: >= 2 })
                return fromGoong;
        }

        return JourneyRoutePointsHelper.FromJourney(j);
    }

    public async Task<JoinJourneyResponse?> JoinByShareCodeAsync(
        string shareCode,
        Guid travelerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode)) return null;

        var row = await _sharedJourneyRepository.GetByShareCodeWithJourneyAsync(shareCode.Trim(), cancellationToken);
        if (row?.Journey == null || !row.IsActive) return null;

        var j = row.Journey;
        if (IsTerminalJourneyStatus(j.Status)) return null;

        var displayName = (await _userProfileRepository.GetByUserIdAsync(travelerId, cancellationToken))?.FullName?.Trim()
            ?? "Thành viên";

        if (j.TravelerId == travelerId)
        {
            var owner = await _journeyMemberRepository.EnsureOwnerMemberAsync(j.Id, travelerId, displayName, cancellationToken);
            return MapJoin(owner, j.Id);
        }

        var existing = await _journeyMemberRepository.GetActiveByTravelerAsync(j.Id, travelerId, cancellationToken);
        if (existing != null)
            return MapJoin(existing, j.Id);

        var member = new JourneyMember
        {
            JourneyId = j.Id,
            TravelerId = travelerId,
            DisplayName = displayName,
            IsRegisteredUser = true,
            Role = JourneyMemberRoles.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        await _journeyMemberRepository.AddAsync(member, cancellationToken);
        return MapJoin(member, j.Id);
    }

    public async Task<JoinJourneyResponse?> JoinGuestByShareCodeAsync(
        string shareCode,
        JoinJourneyGuestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode) || request == null || string.IsNullOrWhiteSpace(request.DisplayName))
            return null;

        var row = await _sharedJourneyRepository.GetByShareCodeWithJourneyAsync(shareCode.Trim(), cancellationToken);
        if (row?.Journey == null || !row.IsActive) return null;

        var j = row.Journey;
        if (IsTerminalJourneyStatus(j.Status)) return null;

        if (request.GuestKey.HasValue)
        {
            var existingGuest = await _journeyMemberRepository.GetActiveByGuestKeyAsync(j.Id, request.GuestKey.Value, cancellationToken);
            if (existingGuest != null)
                return MapJoin(existingGuest, j.Id, guestKey: existingGuest.GuestKey);
        }

        var guestKey = request.GuestKey ?? Guid.NewGuid();
        var member = new JourneyMember
        {
            JourneyId = j.Id,
            GuestKey = guestKey,
            TravelerId = null,
            DisplayName = request.DisplayName.Trim(),
            IsRegisteredUser = false,
            Role = JourneyMemberRoles.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        await _journeyMemberRepository.AddAsync(member, cancellationToken);
        return MapJoin(member, j.Id, guestKey: guestKey);
    }

    public async Task<bool> LeaveJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default)
    {
        var m = await _journeyMemberRepository.GetActiveByTravelerAsync(journeyId, travelerId, cancellationToken);
        if (m == null || m.Role == JourneyMemberRoles.Owner)
            return false;

        m.IsActive = false;
        m.LeftAt = DateTime.UtcNow;
        await _journeyMemberRepository.UpdateAsync(m, cancellationToken);
        return true;
    }

    public async Task<bool> LeaveJourneyGuestAsync(Guid journeyId, Guid guestKey, CancellationToken cancellationToken = default)
    {
        var m = await _journeyMemberRepository.GetActiveByGuestKeyAsync(journeyId, guestKey, cancellationToken);
        if (m == null || m.Role == JourneyMemberRoles.Owner)
            return false;

        m.IsActive = false;
        m.LeftAt = DateTime.UtcNow;
        await _journeyMemberRepository.UpdateAsync(m, cancellationToken);
        return true;
    }

    private static JoinJourneyResponse MapJoin(JourneyMember m, Guid journeyId, Guid? guestKey = null) =>
        new()
        {
            JourneyId = journeyId,
            MemberId = m.Id,
            Role = m.Role,
            DisplayName = m.DisplayName,
            GuestKey = guestKey ?? m.GuestKey
        };

    private static bool IsTerminalJourneyStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        return status.Equals("completed", StringComparison.OrdinalIgnoreCase)
               || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateUniqueShareCodeAsync(CancellationToken cancellationToken)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;

        for (var attempt = 0; attempt < 32; attempt++)
        {
            var code = new string(Enumerable.Range(0, 10).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
            if (!await _sharedJourneyRepository.ShareCodeExistsAsync(code, cancellationToken))
                return code;
        }

        throw new InvalidOperationException("Không tạo được mã chia sẻ duy nhất.");
    }
}
