using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System;

namespace JSEA_Application.Services.Journey;

public class JourneyProgressService : IJourneyProgressService
{
    private const int CompleteJourneyPoints = 5;

    private readonly IJourneyRepository _journeyRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IRewardService _rewardService;

    public JourneyProgressService(
        IJourneyRepository journeyRepository,
        IVisitRepository visitRepository,
        IFeedbackRepository feedbackRepository,
        IRatingRepository ratingRepository,
        IRewardService rewardService)
    {
        _journeyRepository = journeyRepository;
        _visitRepository = visitRepository;
        _feedbackRepository = feedbackRepository;
        _ratingRepository = ratingRepository;
        _rewardService = rewardService;
    }

    public async Task<StartJourneyResponse?> StartJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null) return null;
        if (journey.TravelerId != travelerId) return null;

        if (!journey.StartedAt.HasValue)
        {
            journey.StartedAt = DateTime.UtcNow;
            journey.Status = JourneyStatus.InProgress.ToString();
            journey.UpdatedAt = DateTime.UtcNow;
            await _journeyRepository.UpdateAsync(journey, cancellationToken);
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

        // Defensive parsing: DB may store either enum string (Completed) or snake/lowercase (completed).
        JourneyStatus? status = null;
        if (!string.IsNullOrWhiteSpace(journey.Status) && Enum.TryParse<JourneyStatus>(journey.Status, ignoreCase: true, out var parsed))
            status = parsed;

        if (status == JourneyStatus.Cancelled || string.Equals(journey.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hành trình đã bị hủy, không thể hoàn tất.");

        if (!journey.StartedAt.HasValue)
            throw new InvalidOperationException("Hành trình chưa bắt đầu, không thể hoàn tất.");

        // Idempotent: only award points when transitioning to Completed.
        var shouldAwardPoints = status != JourneyStatus.Completed || !journey.CompletedAt.HasValue;

        // Idempotent: if already completed, return existing timestamp.
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
        }

        return new CompleteJourneyResponse
        {
            JourneyId = journey.Id,
            CompletedAt = journey.CompletedAt!.Value,
            PointsEarned = shouldAwardPoints ? CompleteJourneyPoints : 0
        };
    }

    public async Task<WaypointCheckInResponse?> CheckInAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        request ??= new WaypointCheckInRequest();

        var waypoint = await _journeyRepository.GetWaypointForTravelerAsync(journeyId, waypointId, travelerId, cancellationToken);
        if (waypoint?.Journey == null) return null;

        if (!waypoint.Journey.StartedAt.HasValue)
            return null;

        if (!waypoint.ActualArrivalAt.HasValue)
        {
            waypoint.ActualArrivalAt = DateTime.UtcNow;
            await _journeyRepository.UpdateWaypointAsync(waypoint, cancellationToken);
        }

        var existingVisit = await _visitRepository.GetByJourneyTravelerExperienceAsync(
            journeyId,
            travelerId,
            waypoint.ExperienceId,
            cancellationToken);

        var visit = existingVisit ?? new Visit
        {
            TravelerId = travelerId,
            JourneyId = journeyId,
            ExperienceId = waypoint.ExperienceId,
            VisitedAt = DateTime.UtcNow,
            PhotoUrls = request.PhotoUrls
        };

        if (existingVisit == null)
        {
            visit = await _visitRepository.SaveAsync(visit, cancellationToken);
        }
        else if (request.PhotoUrls is { Count: > 0 })
        {
            // Merge ảnh nếu FE gửi thêm.
            visit.PhotoUrls ??= new List<string>();
            foreach (var url in request.PhotoUrls.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!visit.PhotoUrls.Contains(url))
                    visit.PhotoUrls.Add(url);
            }
            visit = await _visitRepository.SaveAsync(visit, cancellationToken);
        }

        Guid? feedbackId = null;
        if (!string.IsNullOrWhiteSpace(request.FeedbackText))
        {
            var existingFeedback = await _feedbackRepository.GetByVisitIdAsync(visit.Id, cancellationToken);
            if (existingFeedback == null)
            {
                var feedback = await _feedbackRepository.SaveAsync(new Feedback
                {
                    VisitId = visit.Id,
                    FeedbackText = request.FeedbackText.Trim(),
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                feedbackId = feedback.Id;
            }
            else
            {
                // Idempotent: giữ feedback cũ, không overwrite.
                feedbackId = existingFeedback.Id;
            }
        }

        return new WaypointCheckInResponse
        {
            JourneyId = journeyId,
            WaypointId = waypointId,
            VisitId = visit.Id,
            FeedbackId = feedbackId,
            VisitedAt = visit.VisitedAt ?? DateTime.UtcNow,
            ActualArrivalAt = waypoint.ActualArrivalAt
        };
    }

    public async Task<WaypointCheckOutResponse?> CheckOutAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) return null;
        if (request.RatingValue < 1 || request.RatingValue > 5) return null;

        var waypoint = await _journeyRepository.GetWaypointForTravelerAsync(journeyId, waypointId, travelerId, cancellationToken);
        if (waypoint?.Journey == null) return null;

        if (!waypoint.Journey.StartedAt.HasValue)
            return null;

        if (!waypoint.ActualArrivalAt.HasValue)
        {
            // Cho phép checkout trực tiếp: coi như vừa tới.
            waypoint.ActualArrivalAt = DateTime.UtcNow;
        }

        waypoint.ActualDepartureAt = DateTime.UtcNow;

        var stopMinutes = (int)Math.Ceiling((waypoint.ActualDepartureAt.Value - waypoint.ActualArrivalAt.Value).TotalMinutes);
        if (stopMinutes < 0) stopMinutes = 0;

        waypoint.ActualStopMinutes = stopMinutes;
        await _journeyRepository.UpdateWaypointAsync(waypoint, cancellationToken);

        var existingVisit = await _visitRepository.GetByJourneyTravelerExperienceAsync(
            journeyId,
            travelerId,
            waypoint.ExperienceId,
            cancellationToken);

        var visit = existingVisit ?? new Visit
        {
            TravelerId = travelerId,
            JourneyId = journeyId,
            ExperienceId = waypoint.ExperienceId,
            VisitedAt = waypoint.ActualArrivalAt,
            ActualDurationMinutes = stopMinutes
        };

        if (existingVisit == null)
        {
            visit = await _visitRepository.SaveAsync(visit, cancellationToken);
        }
        else
        {
            visit.ActualDurationMinutes = stopMinutes;
            visit = await _visitRepository.SaveAsync(visit, cancellationToken);
        }

        var existingRating = await _ratingRepository.GetByVisitIdAsync(visit.Id, cancellationToken);
        var rating = existingRating ?? await _ratingRepository.SaveAsync(new Rating
        {
            VisitId = visit.Id,
            Rating1 = request.RatingValue,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new WaypointCheckOutResponse
        {
            JourneyId = journeyId,
            WaypointId = waypointId,
            VisitId = visit.Id,
            RatingId = rating.Id,
            ActualDepartureAt = waypoint.ActualDepartureAt,
            ActualStopMinutes = waypoint.ActualStopMinutes
        };
    }
}
