using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IJourneyRepository
{
    Task<Journey> SaveAsync(Journey journey, List<JourneyWaypoint> waypoints, CancellationToken cancellationToken = default);
    Task<Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
