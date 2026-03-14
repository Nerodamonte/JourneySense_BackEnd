using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Models;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Interfaces;

public interface IMicroExperienceRepository
{
    Task<List<Experience>> FindAllAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default);
    Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Experience> SaveAsync(Experience entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);


    /// <summary>Đếm số experiences active gần một tuyến bất kỳ (theo routePath, maxDetourDistanceMeters, vehicleType...). Không dùng journeys.</summary>
    Task<int> CountAlongRouteAsync(NetTopologySuite.Geometries.LineString? routePath, int maxDetourDistanceMeters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard filter cho suggest pipeline v11.
    /// Lọc theo: status=active, accessible_by, crowd_level, ST_Distance, excludeIds, opening_hours.
    /// Trả về list Experience kèm Detail + Metric + Photos + Category.
    /// </summary>
    Task<List<Experience>> FindCandidatesAsync(
        string vehicleType,
        string preferredCrowdLevel,
        LineString segmentPath,
        int maxDetourDistanceMeters,
        List<Guid> excludeIds,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy active event boost theo danh sách experience_id tại thời điểm hiện tại.</summary>
    Task<Dictionary<Guid, decimal>> GetActiveEventBoostsAsync(List<Guid> experienceIds, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả experiences active chưa có embedding, kèm Category + Detail. Dùng cho EmbeddingGeneratorService.</summary>
    Task<List<Experience>> GetActiveWithoutEmbeddingAsync(CancellationToken cancellationToken = default);
}
