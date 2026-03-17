using JSEA_Application.Models;
using JSEA_Application.Enums;

namespace JSEA_Application.Interfaces;

public interface IJourneyRepository
{
    Task<Journey> SaveAsync(Journey journey, List<JourneyWaypoint> waypoints, List<RouteSegment>? segments = null, CancellationToken cancellationToken = default);
    Task<Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Journey>> GetByTravelerIdAsync(Guid travelerId, CancellationToken cancellationToken = default);
    /// <summary>Lấy danh sách experience_id đã được gợi ý trong journey (tránh suggest trùng).</summary>
    /// <summary>Lấy experience_id đã suggest trong segment này (tránh suggest trùng trên cùng 1 tuyến).</summary>
    Task<List<Guid>> GetSuggestedExperienceIdsAsync(Guid journeyId, Guid segmentId, CancellationToken cancellationToken = default);

    /// <summary>Tổng số phút đã dùng = Σ planned_stop_minutes + Σ detour_time_minutes của các waypoint đã accept.</summary>
    Task<int> GetUsedMinutesAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Đếm số waypoint đã accept trong journey (để check max_stops).</summary>
    Task<int> GetAcceptedWaypointCountAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Lưu một JourneySuggestion mới.</summary>
    Task<JourneySuggestion> SaveSuggestionAsync(JourneySuggestion suggestion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu nhiều JourneySuggestion trong 1 lần SaveChanges (hiệu năng tốt hơn khi trả về full list).
    /// </summary>
    Task SaveSuggestionsAsync(IEnumerable<JourneySuggestion> suggestions, CancellationToken cancellationToken = default);

    /// <summary>Lấy JourneySuggestion theo id, kèm Experience + Detail + Metric và Journey.</summary>
    Task<JourneySuggestion?> GetSuggestionByIdAsync(Guid suggestionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy nhiều suggestions theo danh sách id (phục vụ lưu waypoint/validate suggestion thuộc journey).
    /// </summary>
    Task<List<JourneySuggestion>> GetSuggestionsByIdsAsync(IEnumerable<Guid> suggestionIds, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật ai_insight của một suggestion sau khi RAG generate xong.</summary>
    Task UpdateSuggestionInsightAsync(Guid suggestionId, string insight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace toàn bộ waypoint của journey (xóa cũ, thêm mới) và có thể ghi interactions.
    /// </summary>
    Task ReplaceWaypointsAsync(
        Guid journeyId,
        IEnumerable<JourneyWaypoint> newWaypoints,
        IEnumerable<SuggestionInteraction>? newInteractions = null,
        CancellationToken cancellationToken = default);

    /// <summary>Ghi nhận một interaction cho suggestion.</summary>
    Task AddSuggestionInteractionAsync(Guid suggestionId, InteractionType interactionType, CancellationToken cancellationToken = default);
}