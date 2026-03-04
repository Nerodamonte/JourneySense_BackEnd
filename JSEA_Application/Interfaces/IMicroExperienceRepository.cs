using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IMicroExperienceRepository
{
    Task<List<Experience>> FindAllAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default);
    Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Experience> SaveAsync(Experience entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Experiences gần tuyến (trong bán kính detour), lọc theo mood, weather, timeOfDay và status. Sắp xếp theo khoảng cách.</summary>
    Task<List<RouteMicroExperienceSuggestionResponse>> FindSuggestionsAlongRouteAsync(Guid journeyId, int limit, string? weatherStr, string? timeOfDayStr, CancellationToken cancellationToken = default);

    /// <summary>Đếm số experiences active gần một tuyến bất kỳ (theo routePath, maxDetourDistanceMeters, vehicleType...). Không dùng journeys.</summary>
    Task<int> CountAlongRouteAsync(NetTopologySuite.Geometries.LineString? routePath, int maxDetourDistanceMeters, CancellationToken cancellationToken = default);
}
