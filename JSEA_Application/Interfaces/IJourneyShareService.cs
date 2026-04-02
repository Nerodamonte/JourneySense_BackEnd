using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;

namespace JSEA_Application.Interfaces;

public interface IJourneyShareService
{
    Task<ShareJourneyResponse?> ShareJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<PublicSharedJourneyResponse?> GetPublicByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default);

    Task<List<PublicSharedJourneyListItemResponse>> GetPublicSharedJourneysAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PublicSharedJourneyDetailResponse?> GetPublicDetailByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default);

    Task<JoinJourneyResponse?> JoinByShareCodeAsync(string shareCode, Guid travelerId, CancellationToken cancellationToken = default);

    Task<JoinJourneyResponse?> JoinGuestByShareCodeAsync(string shareCode, JoinJourneyGuestRequest request, CancellationToken cancellationToken = default);

    Task<bool> LeaveJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<bool> LeaveJourneyGuestAsync(Guid journeyId, Guid guestKey, CancellationToken cancellationToken = default);
}
