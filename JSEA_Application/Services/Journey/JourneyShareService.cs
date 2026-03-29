using JSEA_Application.Constants;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

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

    public JourneyShareService(
        IJourneyRepository journeyRepository,
        ISharedJourneyRepository sharedJourneyRepository,
        IAchievementRepository achievementRepository,
        IRewardService rewardService,
        IUserProfileRepository userProfileRepository,
        IVisitRepository visitRepository,
        IFeedbackRepository feedbackRepository,
        IRatingRepository ratingRepository)
    {
        _journeyRepository = journeyRepository;
        _sharedJourneyRepository = sharedJourneyRepository;
        _achievementRepository = achievementRepository;
        _rewardService = rewardService;
        _userProfileRepository = userProfileRepository;
        _visitRepository = visitRepository;
        _feedbackRepository = feedbackRepository;
        _ratingRepository = ratingRepository;
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
            PointsEarned = points
        };
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
            Waypoints = waypointResponses
        };
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
