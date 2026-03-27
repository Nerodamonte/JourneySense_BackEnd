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

    public JourneyShareService(
        IJourneyRepository journeyRepository,
        ISharedJourneyRepository sharedJourneyRepository,
        IAchievementRepository achievementRepository,
        IRewardService rewardService)
    {
        _journeyRepository = journeyRepository;
        _sharedJourneyRepository = sharedJourneyRepository;
        _achievementRepository = achievementRepository;
        _rewardService = rewardService;
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
