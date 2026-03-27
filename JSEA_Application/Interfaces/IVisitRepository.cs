using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IVisitRepository
{
    Task<bool> ExistsVisitAsync(Guid travelerId, Guid experienceId, Guid journeyId, CancellationToken cancellationToken = default);
    Task<Visit> SaveAsync(Visit visit, CancellationToken cancellationToken = default);
    Task<Visit?> GetByJourneyTravelerExperienceAsync(Guid journeyId, Guid travelerId, Guid experienceId, CancellationToken cancellationToken = default);

    Task<List<Visit>> GetByJourneyTravelerAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);
}
