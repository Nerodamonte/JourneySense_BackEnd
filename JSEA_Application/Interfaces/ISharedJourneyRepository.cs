using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface ISharedJourneyRepository
{
    Task<SharedJourney?> GetActiveByJourneyAndUserAsync(
        Guid journeyId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SharedJourney?> GetByShareCodeWithJourneyAsync(string shareCode, CancellationToken cancellationToken = default);

    Task<bool> ShareCodeExistsAsync(string shareCode, CancellationToken cancellationToken = default);

    Task<SharedJourney> AddAsync(SharedJourney entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(SharedJourney entity, CancellationToken cancellationToken = default);
}
