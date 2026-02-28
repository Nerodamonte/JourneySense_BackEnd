using JSEA_Application.DTOs.Request.Experience;
using JSEA_Application.DTOs.Respone.Experience;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Experience;

public class RateFeedbackService : IRateFeedbackService
{
    private const int PointsPerVisit = 10;

    private readonly IVisitRepository _visitRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IRewardService _rewardService;

    public RateFeedbackService(
        IVisitRepository visitRepository,
        IRatingRepository ratingRepository,
        IFeedbackRepository feedbackRepository,
        IRewardService rewardService)
    {
        _visitRepository = visitRepository;
        _ratingRepository = ratingRepository;
        _feedbackRepository = feedbackRepository;
        _rewardService = rewardService;
    }

    public async Task<VisitFeedbackResponse?> CreateVisitWithFeedbackAsync(VisitFeedbackRequest request, Guid travelerId, CancellationToken cancellationToken = default)
    {
        if (await _visitRepository.ExistsVisitAsync(travelerId, request.ExperienceId, request.JourneyId, cancellationToken))
            return null;

        var visit = new Visit
        {
            TravelerId = travelerId,
            ExperienceId = request.ExperienceId,
            JourneyId = request.JourneyId,
            PhotoUrls = request.PhotoUrls
        };
        visit = await _visitRepository.SaveAsync(visit, cancellationToken);

        await _rewardService.AddRewardPointsAsync(travelerId, PointsPerVisit, "mark_visited", cancellationToken);

        Guid? ratingId = null;
        if (request.RatingValue >= 1 && request.RatingValue <= 5)
        {
            var rating = new Rating
            {
                VisitId = visit.Id,
                TravelerId = travelerId,
                ExperienceId = request.ExperienceId,
                Rating1 = request.RatingValue
            };
            rating = await _ratingRepository.SaveAsync(rating, cancellationToken);
            ratingId = rating.Id;
        }

        Guid? feedbackId = null;
        if (!string.IsNullOrWhiteSpace(request.FeedbackText) || (request.PhotoUrls?.Count > 0))
        {
            var feedback = new Feedback
            {
                VisitId = visit.Id,
                TravelerId = travelerId,
                ExperienceId = request.ExperienceId,
                FeedbackText = request.FeedbackText?.Trim()
            };
            feedback = await _feedbackRepository.SaveAsync(feedback, cancellationToken);
            feedbackId = feedback.Id;
        }

        return new VisitFeedbackResponse
        {
            VisitId = visit.Id,
            RatingId = ratingId,
            FeedbackId = feedbackId,
            PointsEarned = PointsPerVisit
        };
    }
}
