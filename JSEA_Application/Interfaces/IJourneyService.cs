using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;

namespace JSEA_Application.Interfaces;

public interface IJourneyService
{
    /// <summary>
    /// Validate request, gọi Goong phân tích tuyến, lưu Journey + waypoints, trả về response.
    /// </summary>
    Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hành trình của traveler (lịch sử micro-journey). travelerId null trả về rỗng.
    /// </summary>
    Task<List<JourneyListItemResponse>> GetMyJourneysAsync(Guid? travelerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết một journey theo id.
    /// </summary>
    Task<JourneyDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu danh sách các điểm user chọn ghé (waypoints) cho một journey theo segment (route) đã chọn.
    /// totalTimeBudget = base route minutes (Goong) + Σ detour + Σ plannedStopMinutes.
    /// Replace toàn bộ waypoints hiện tại.
    /// </summary>
    Task<bool> SaveSelectedWaypointsAsync(
        Guid journeyId,
        Guid travelerId,
        Guid segmentId,
        List<SaveWaypointItemRequest> waypoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhận tương tác của user với một suggestion (ViewedDetails/Saved/Accepted/Skipped...).
    /// </summary>
    Task<bool> LogSuggestionInteractionAsync(
        Guid suggestionId,
        Guid travelerId,
        InteractionType interactionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gợi ý micro-experiences dọc/gần tuyến theo journey: lọc theo vibe (current_mood), weather, timeOfDay, khoảng cách lệch, status. Sắp xếp theo khoảng cách.
    /// </summary>
   
}
