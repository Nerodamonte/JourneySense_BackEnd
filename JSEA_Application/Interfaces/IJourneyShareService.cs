using JSEA_Application.DTOs.Respone.Journey;

namespace JSEA_Application.Interfaces;

public interface IJourneyShareService
{
    Task<ShareJourneyResponse?> ShareJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<PublicSharedJourneyResponse?> GetPublicByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default);
}
