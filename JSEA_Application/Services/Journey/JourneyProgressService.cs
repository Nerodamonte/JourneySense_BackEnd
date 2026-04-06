using JSEA_Application.Constants;
using JSEA_Application.DTOs.Journey;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System;
using JourneyEntity = JSEA_Application.Models.Journey;

namespace JSEA_Application.Services.Journey;

public class JourneyProgressService : IJourneyProgressService
{
    private const int CompleteJourneyPoints = 5;

    private readonly IJourneyRepository _journeyRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IExperienceMetricRepository _experienceMetrics;
    private readonly IRewardService _rewardService;
    private readonly IUserPackageRepository _userPackageRepository;
    private readonly IJourneyMemberRepository _journeyMemberRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IJourneyLiveNotifier _journeyLiveNotifier;

    public JourneyProgressService(
        IJourneyRepository journeyRepository,
        IVisitRepository visitRepository,
        IFeedbackRepository feedbackRepository,
        IRatingRepository ratingRepository,
        IExperienceMetricRepository experienceMetrics,
        IRewardService rewardService,
        IUserPackageRepository userPackageRepository,
        IJourneyMemberRepository journeyMemberRepository,
        IUserProfileRepository userProfileRepository,
        IJourneyLiveNotifier journeyLiveNotifier)
    {
        _journeyRepository = journeyRepository;
        _visitRepository = visitRepository;
        _feedbackRepository = feedbackRepository;
        _ratingRepository = ratingRepository;
        _experienceMetrics = experienceMetrics;
        _rewardService = rewardService;
        _userPackageRepository = userPackageRepository;
        _journeyMemberRepository = journeyMemberRepository;
        _userProfileRepository = userProfileRepository;
        _journeyLiveNotifier = journeyLiveNotifier;
    }

    public async Task<StartJourneyResponse?> StartJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null) return null;
        if (journey.TravelerId != travelerId) return null;

        var firstStart = !journey.StartedAt.HasValue;
        if (firstStart)
        {
            journey.StartedAt = DateTime.UtcNow;
            journey.Status = JourneyStatus.InProgress.ToString();
            journey.UpdatedAt = DateTime.UtcNow;
            await _journeyRepository.UpdateAsync(journey, cancellationToken);
        }

        var name = (await _userProfileRepository.GetByUserIdAsync(travelerId, cancellationToken))?.FullName?.Trim() ?? "Owner";
        await _journeyMemberRepository.EnsureOwnerMemberAsync(journeyId, travelerId, name, cancellationToken);

        if (firstStart)
        {
            await _journeyLiveNotifier.NotifyJourneyStartedAsync(
                new JourneyStartedLiveNotification
                {
                    JourneyId = journey.Id,
                    StartedAt = journey.StartedAt!.Value
                },
                cancellationToken);
        }

        return new StartJourneyResponse
        {
            JourneyId = journey.Id,
            StartedAt = journey.StartedAt!.Value
        };
    }

    public async Task<CompleteJourneyResponse?> CompleteJourneyAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null) return null;
        if (journey.TravelerId != travelerId) return null;

        JourneyStatus? status = null;
        if (!string.IsNullOrWhiteSpace(journey.Status) && Enum.TryParse<JourneyStatus>(journey.Status, ignoreCase: true, out var parsed))
            status = parsed;

        if (status == JourneyStatus.Cancelled || string.Equals(journey.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hành trình đã bị hủy, không thể hoàn tất.");

        if (!journey.StartedAt.HasValue)
            throw new InvalidOperationException("Hành trình chưa bắt đầu, không thể hoàn tất.");

        var isSolo = await _journeyMemberRepository.IsSoloOwnerOnlyActiveRosterAsync(journeyId, cancellationToken);
        if (!isSolo &&
            !await _journeyMemberRepository.AllActiveMembersConfirmedAtDestinationAsync(journeyId, cancellationToken))
            throw new InvalidOperationException(
                "Chưa thể hoàn tất: mọi thành viên đang tham gia cần xác nhận đã tới đích (điểm đến).");

        var shouldAwardPoints = status != JourneyStatus.Completed || !journey.CompletedAt.HasValue;

        if (shouldAwardPoints)
        {
            journey.Status = JourneyStatus.Completed.ToString();
            journey.CompletedAt ??= DateTime.UtcNow;
            journey.UpdatedAt = DateTime.UtcNow;
            await _journeyRepository.UpdateAsync(journey, cancellationToken);

            await _rewardService.AddRewardPointsAsync(
                travelerId,
                CompleteJourneyPoints,
                "complete_journey",
                cancellationToken,
                achievementId: null,
                refId: journeyId,
                refType: "journey");

            var meters = journey.ActualDistanceMeters ?? journey.TotalDistanceMeters ?? 0;
            var deltaKm = meters / 1000m;
            await _userPackageRepository.AddUsedKmToActivePackageAsync(
                travelerId,
                deltaKm,
                DateTime.UtcNow,
                cancellationToken);
        }

        return new CompleteJourneyResponse
        {
            JourneyId = journey.Id,
            CompletedAt = journey.CompletedAt!.Value,
            PointsEarned = shouldAwardPoints ? CompleteJourneyPoints : 0
        };
    }

    public Task<WaypointCheckInResponse?> CheckInAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken = default) =>
        CheckInCoreAsync(journeyId, waypointId, travelerId, guestKey: null, request, cancellationToken);

    public Task<WaypointCheckInResponse?> CheckInGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken = default) =>
        CheckInCoreAsync(journeyId, waypointId, travelerId: null, guestKey, request, cancellationToken);

    private async Task<WaypointCheckInResponse?> CheckInCoreAsync(
        Guid journeyId,
        Guid waypointId,
        Guid? travelerId,
        Guid? guestKey,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new WaypointCheckInRequest();

        var waypoint = guestKey.HasValue
            ? await _journeyRepository.GetWaypointForJourneyAsync(journeyId, waypointId, cancellationToken)
            : await _journeyRepository.GetWaypointForTravelerAsync(journeyId, waypointId, travelerId!.Value, cancellationToken);
        if (waypoint?.Journey == null) return null;

        if (!waypoint.Journey.StartedAt.HasValue)
            return null;

        var member = await ResolveMemberAsync(journeyId, travelerId, guestKey, waypoint.Journey, cancellationToken);
        if (member == null) return null;

        var isOwner = waypoint.Journey.TravelerId == member.TravelerId;

        if (isOwner && member.TravelerId.HasValue)
        {
            if (!waypoint.ActualArrivalAt.HasValue)
            {
                waypoint.ActualArrivalAt = DateTime.UtcNow;
                await _journeyRepository.UpdateWaypointAsync(waypoint, cancellationToken);
            }
        }

        await UpsertWaypointArrivedAsync(member.Id, waypointId, DateTime.UtcNow, cancellationToken);

        Visit? visit = null;
        Guid? feedbackId = null;

        if (member.IsRegisteredUser && member.TravelerId.HasValue)
        {
            var tid = member.TravelerId.Value;
            var existingVisit = await _visitRepository.GetByJourneyTravelerExperienceAsync(
                journeyId,
                tid,
                waypoint.ExperienceId,
                cancellationToken);

            visit = existingVisit ?? new Visit
            {
                TravelerId = tid,
                JourneyId = journeyId,
                ExperienceId = waypoint.ExperienceId,
                VisitedAt = DateTime.UtcNow,
                PhotoUrls = request.PhotoUrls
            };

            if (existingVisit == null)
            {
                visit = await _visitRepository.SaveAsync(visit, cancellationToken);
                await _experienceMetrics.IncrementVisitCountAsync(waypoint.ExperienceId, cancellationToken);
            }
            else if (request.PhotoUrls is { Count: > 0 })
            {
                visit.PhotoUrls ??= new List<string>();
                foreach (var url in request.PhotoUrls.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!visit.PhotoUrls.Contains(url))
                        visit.PhotoUrls.Add(url);
                }

                visit = await _visitRepository.SaveAsync(visit, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(request.FeedbackText))
            {
                var existingFeedback = await _feedbackRepository.GetByVisitIdAsync(visit.Id, cancellationToken);
                if (existingFeedback == null)
                {
                    var feedback = await _feedbackRepository.SaveAsync(new Feedback
                    {
                        VisitId = visit.Id,
                        FeedbackText = request.FeedbackText.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        ModerationStatus = FeedbackModerationStatuses.Pending,
                        IsFlagged = false
                    }, cancellationToken);
                    feedbackId = feedback.Id;
                }
                else
                {
                    feedbackId = existingFeedback.Id;
                }
            }
        }

        var progress = await _journeyMemberRepository.GetProgressAsync(
            member.Id, waypointId, JourneyMilestoneKinds.Waypoint, cancellationToken);
        var actualArrival = waypoint.ActualArrivalAt ?? progress?.ArrivedAt;

        await BroadcastWaypointAttendanceAsync(journeyId, cancellationToken);

        return new WaypointCheckInResponse
        {
            JourneyId = journeyId,
            WaypointId = waypointId,
            VisitId = visit?.Id ?? Guid.Empty,
            FeedbackId = feedbackId,
            VisitedAt = visit?.VisitedAt ?? DateTime.UtcNow,
            ActualArrivalAt = actualArrival
        };
    }

    public Task<WaypointCheckOutResponse?> CheckOutAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken = default) =>
        CheckOutCoreAsync(journeyId, waypointId, travelerId, null, request, cancellationToken);

    public Task<WaypointCheckOutResponse?> CheckOutGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken = default) =>
        CheckOutCoreAsync(journeyId, waypointId, null, guestKey, request, cancellationToken);

    private async Task<WaypointCheckOutResponse?> CheckOutCoreAsync(
        Guid journeyId,
        Guid waypointId,
        Guid? travelerId,
        Guid? guestKey,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null) return null;
        if (request.RatingValue is { } rv && (rv < 1 || rv > 5)) return null;

        var waypoint = guestKey.HasValue
            ? await _journeyRepository.GetWaypointForJourneyAsync(journeyId, waypointId, cancellationToken)
            : await _journeyRepository.GetWaypointForTravelerAsync(journeyId, waypointId, travelerId!.Value, cancellationToken);
        if (waypoint?.Journey == null) return null;

        if (!waypoint.Journey.StartedAt.HasValue)
            return null;

        var member = await ResolveMemberAsync(journeyId, travelerId, guestKey, waypoint.Journey, cancellationToken);
        if (member == null) return null;

        var isOwner = waypoint.Journey.TravelerId == member.TravelerId;

        if (isOwner && member.TravelerId.HasValue)
        {
            if (!waypoint.ActualArrivalAt.HasValue)
                waypoint.ActualArrivalAt = DateTime.UtcNow;

            waypoint.ActualDepartureAt = DateTime.UtcNow;

            var stopMinutesOwner = (int)Math.Ceiling((waypoint.ActualDepartureAt.Value - (waypoint.ActualArrivalAt ?? waypoint.ActualDepartureAt.Value)).TotalMinutes);
            if (stopMinutesOwner < 0) stopMinutesOwner = 0;
            waypoint.ActualStopMinutes = stopMinutesOwner;
            await _journeyRepository.UpdateWaypointAsync(waypoint, cancellationToken);
        }

        var at = DateTime.UtcNow;
        var progressRow = await _journeyMemberRepository.GetProgressAsync(member.Id, waypointId, JourneyMilestoneKinds.Waypoint, cancellationToken)
                          ?? new JourneyWaypointMemberProgress
                          {
                              JourneyMemberId = member.Id,
                              JourneyWaypointId = waypointId,
                              MilestoneKind = JourneyMilestoneKinds.Waypoint
                          };

        progressRow.ArrivedAt ??= at;
        progressRow.DepartedAt = at;
        progressRow.Skipped = false;
        await _journeyMemberRepository.SaveProgressAsync(progressRow, cancellationToken);

        var stopMinutes = progressRow.ArrivedAt.HasValue && progressRow.DepartedAt.HasValue
            ? (int)Math.Ceiling((progressRow.DepartedAt.Value - progressRow.ArrivedAt.Value).TotalMinutes)
            : 0;
        if (stopMinutes < 0) stopMinutes = 0;

        Guid? ratingId = null;
        Guid visitId = Guid.Empty;

        if (member.IsRegisteredUser && member.TravelerId.HasValue)
        {
            var tid = member.TravelerId.Value;
            var existingVisit = await _visitRepository.GetByJourneyTravelerExperienceAsync(
                journeyId,
                tid,
                waypoint.ExperienceId,
                cancellationToken);

            var visit = existingVisit ?? new Visit
            {
                TravelerId = tid,
                JourneyId = journeyId,
                ExperienceId = waypoint.ExperienceId,
                VisitedAt = progressRow.ArrivedAt,
                ActualDurationMinutes = stopMinutes
            };

            if (existingVisit == null)
            {
                visit = await _visitRepository.SaveAsync(visit, cancellationToken);
                await _experienceMetrics.IncrementVisitCountAsync(waypoint.ExperienceId, cancellationToken);
            }
            else
            {
                visit.ActualDurationMinutes = stopMinutes;
                visit = await _visitRepository.SaveAsync(visit, cancellationToken);
            }

            visitId = visit.Id;

            if (request.RatingValue is >= 1 and <= 5)
            {
                var stars = request.RatingValue.Value;
                var existingRating = await _ratingRepository.GetByVisitIdAsync(visit.Id, cancellationToken);
                if (existingRating == null)
                {
                    var rating = await _ratingRepository.SaveAsync(new Rating
                    {
                        VisitId = visit.Id,
                        Rating1 = stars,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                    await _experienceMetrics.AddRatingAsync(waypoint.ExperienceId, stars, cancellationToken);
                    ratingId = rating.Id;
                }
                else
                {
                    ratingId = existingRating.Id;
                }
            }
        }

        await BroadcastWaypointAttendanceAsync(journeyId, cancellationToken);

        return new WaypointCheckOutResponse
        {
            JourneyId = journeyId,
            WaypointId = waypointId,
            VisitId = visitId,
            RatingId = ratingId,
            ActualDepartureAt = progressRow.DepartedAt,
            ActualStopMinutes = stopMinutes
        };
    }

    public Task<WaypointSkipResponse?> SkipWaypointAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        CancellationToken cancellationToken = default) =>
        SkipWaypointCoreAsync(journeyId, waypointId, travelerId, null, cancellationToken);

    public Task<WaypointSkipResponse?> SkipWaypointGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        CancellationToken cancellationToken = default) =>
        SkipWaypointCoreAsync(journeyId, waypointId, null, guestKey, cancellationToken);

    private async Task<WaypointSkipResponse?> SkipWaypointCoreAsync(
        Guid journeyId,
        Guid waypointId,
        Guid? travelerId,
        Guid? guestKey,
        CancellationToken cancellationToken)
    {
        var waypoint = guestKey.HasValue
            ? await _journeyRepository.GetWaypointForJourneyAsync(journeyId, waypointId, cancellationToken)
            : await _journeyRepository.GetWaypointForTravelerAsync(journeyId, waypointId, travelerId!.Value, cancellationToken);
        if (waypoint?.Journey == null) return null;

        if (!waypoint.Journey.StartedAt.HasValue)
            return null;

        var member = await ResolveMemberAsync(journeyId, travelerId, guestKey, waypoint.Journey, cancellationToken);
        if (member == null) return null;

        var isOwner = waypoint.Journey.TravelerId == member.TravelerId;

        if (waypoint.ActualDepartureAt.HasValue && isOwner)
        {
            await BroadcastWaypointAttendanceAsync(journeyId, cancellationToken);
            return new WaypointSkipResponse
            {
                JourneyId = journeyId,
                WaypointId = waypointId,
                ActualArrivalAt = waypoint.ActualArrivalAt,
                ActualDepartureAt = waypoint.ActualDepartureAt,
                ActualStopMinutes = waypoint.ActualStopMinutes
            };
        }

        var now = DateTime.UtcNow;

        if (isOwner)
        {
            waypoint.ActualArrivalAt ??= now;
            waypoint.ActualDepartureAt = now;
            waypoint.ActualStopMinutes = 0;
            await _journeyRepository.UpdateWaypointAsync(waypoint, cancellationToken);
        }

        var row = await _journeyMemberRepository.GetProgressAsync(member.Id, waypointId, JourneyMilestoneKinds.Waypoint, cancellationToken)
                 ?? new JourneyWaypointMemberProgress
                 {
                     JourneyMemberId = member.Id,
                     JourneyWaypointId = waypointId,
                     MilestoneKind = JourneyMilestoneKinds.Waypoint
                 };

        row.Skipped = true;
        row.ArrivedAt ??= now;
        row.DepartedAt = now;
        await _journeyMemberRepository.SaveProgressAsync(row, cancellationToken);

        await BroadcastWaypointAttendanceAsync(journeyId, cancellationToken);

        return new WaypointSkipResponse
        {
            JourneyId = journeyId,
            WaypointId = waypointId,
            ActualArrivalAt = isOwner ? waypoint.ActualArrivalAt : row.ArrivedAt,
            ActualDepartureAt = isOwner ? waypoint.ActualDepartureAt : row.DepartedAt,
            ActualStopMinutes = isOwner ? waypoint.ActualStopMinutes : 0
        };
    }

    public Task<DestinationCheckpointResponse?> DestinationCheckInAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default) =>
        DestinationCheckInCoreAsync(journeyId, travelerId, null, cancellationToken);

    public Task<DestinationCheckpointResponse?> DestinationCheckInGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default) =>
        DestinationCheckInCoreAsync(journeyId, null, guestKey, cancellationToken);

    private async Task<DestinationCheckpointResponse?> DestinationCheckInCoreAsync(
        Guid journeyId,
        Guid? travelerId,
        Guid? guestKey,
        CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null || !journey.StartedAt.HasValue) return null;

        var member = await ResolveMemberAsync(journeyId, travelerId, guestKey, journey, cancellationToken);
        if (member == null) return null;

        var row = await _journeyMemberRepository.GetProgressAsync(member.Id, null, JourneyMilestoneKinds.Destination, cancellationToken)
                 ?? new JourneyWaypointMemberProgress
                 {
                     JourneyMemberId = member.Id,
                     JourneyWaypointId = null,
                     MilestoneKind = JourneyMilestoneKinds.Destination
                 };

        var firstArrival = !row.ArrivedAt.HasValue;
        row.ArrivedAt ??= DateTime.UtcNow;
        await _journeyMemberRepository.SaveProgressAsync(row, cancellationToken);

        if (firstArrival)
        {
            await _journeyLiveNotifier.NotifyDestinationMemberArrivedAsync(
                new JourneyDestinationArrivedNotification
                {
                    JourneyId = journeyId,
                    MemberId = member.Id,
                    DisplayName = member.DisplayName,
                    Role = member.Role,
                    IsGuest = !member.IsRegisteredUser,
                    ArrivedAt = row.ArrivedAt.Value
                },
                cancellationToken);
        }

        return new DestinationCheckpointResponse { JourneyId = journeyId, ArrivedAt = row.ArrivedAt, DepartedAt = row.DepartedAt };
    }

    public Task<DestinationCheckpointResponse?> DestinationCheckOutAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default) =>
        DestinationCheckOutCoreAsync(journeyId, travelerId, null, cancellationToken);

    public Task<DestinationCheckpointResponse?> DestinationCheckOutGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default) =>
        DestinationCheckOutCoreAsync(journeyId, null, guestKey, cancellationToken);

    private async Task<DestinationCheckpointResponse?> DestinationCheckOutCoreAsync(
        Guid journeyId,
        Guid? travelerId,
        Guid? guestKey,
        CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null || !journey.StartedAt.HasValue) return null;

        var member = await ResolveMemberAsync(journeyId, travelerId, guestKey, journey, cancellationToken);
        if (member == null) return null;

        var row = await _journeyMemberRepository.GetProgressAsync(member.Id, null, JourneyMilestoneKinds.Destination, cancellationToken)
                 ?? new JourneyWaypointMemberProgress
                 {
                     JourneyMemberId = member.Id,
                     JourneyWaypointId = null,
                     MilestoneKind = JourneyMilestoneKinds.Destination
                 };

        row.ArrivedAt ??= DateTime.UtcNow;
        row.DepartedAt = DateTime.UtcNow;
        row.Skipped = false;
        await _journeyMemberRepository.SaveProgressAsync(row, cancellationToken);

        return new DestinationCheckpointResponse { JourneyId = journeyId, ArrivedAt = row.ArrivedAt, DepartedAt = row.DepartedAt };
    }

    private async Task<JourneyMember?> ResolveMemberAsync(
        Guid journeyId,
        Guid? travelerId,
        Guid? guestKey,
        JourneyEntity journey,
        CancellationToken cancellationToken)
    {
        if (guestKey.HasValue)
            return await _journeyMemberRepository.GetActiveByGuestKeyAsync(journeyId, guestKey.Value, cancellationToken);

        if (!travelerId.HasValue) return null;

        var member = await _journeyMemberRepository.GetActiveByTravelerAsync(journeyId, travelerId.Value, cancellationToken);
        if (member != null) return member;

        if (journey.TravelerId == travelerId.Value)
        {
            var name = (await _userProfileRepository.GetByUserIdAsync(travelerId.Value, cancellationToken))?.FullName?.Trim() ?? "Owner";
            return await _journeyMemberRepository.EnsureOwnerMemberAsync(journeyId, travelerId.Value, name, cancellationToken);
        }

        return null;
    }

    private async Task UpsertWaypointArrivedAsync(Guid journeyMemberId, Guid waypointId, DateTime at, CancellationToken cancellationToken)
    {
        var row = await _journeyMemberRepository.GetProgressAsync(journeyMemberId, waypointId, JourneyMilestoneKinds.Waypoint, cancellationToken)
                 ?? new JourneyWaypointMemberProgress
                 {
                     JourneyMemberId = journeyMemberId,
                     JourneyWaypointId = waypointId,
                     MilestoneKind = JourneyMilestoneKinds.Waypoint
                 };

        row.ArrivedAt ??= at;
        await _journeyMemberRepository.SaveProgressAsync(row, cancellationToken);
    }

    private async Task BroadcastWaypointAttendanceAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var snapshot = await _journeyMemberRepository.GetWaypointAttendanceAsync(journeyId, cancellationToken);
        await _journeyLiveNotifier.NotifyWaypointAttendanceUpdatedAsync(snapshot, cancellationToken);
    }
}
