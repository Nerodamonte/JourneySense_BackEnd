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
    /// Lấy chi tiết một journey theo id. <paramref name="viewerTravelerId"/>: nếu trùng chủ chuyến, xem được feedback chưa duyệt.
    /// </summary>
    Task<JourneyDetailResponse?> GetByIdAsync(Guid id, Guid? viewerTravelerId, CancellationToken cancellationToken = default);

    Task<bool> UpdateJourneyFeedbackAsync(
        Guid journeyId,
        Guid travelerId,
        string? journeyFeedback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu danh sách các điểm user chọn ghé (waypoints) cho một journey theo segment (route) đã chọn.
    /// Time budget check = base route minutes (Goong) + Σ detour minutes.
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
    /// Cập nhật current_mood của journey trong giai đoạn planning.
    /// Khi đổi mood, sẽ xóa cache suggestions để lần gọi suggest kế tiếp regenerate theo mood mới.
    /// </summary>
    Task<bool> UpdateCurrentMoodAsync(
        Guid journeyId,
        Guid travelerId,
        MoodType? currentMood,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gợi ý micro-experiences dọc/gần tuyến theo journey: lọc theo vibe (current_mood), weather, timeOfDay, khoảng cách lệch, status. Sắp xếp theo khoảng cách.
    /// </summary>

    /// <summary>
    /// Lấy polyline tuyến đi qua các waypoint đã chọn (theo StopOrder) để FE vẽ map.
    /// </summary>
    Task<JourneyPolylineResponse?> GetJourneyPolylineAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy polyline từ vị trí hiện tại của user → waypoint gần nhất trong danh sách waypoint đã lưu của journey.
    /// Mục tiêu: mỗi lần user di chuyển, FE gọi lại endpoint để nhận tuyến đi đến waypoint gần nhất.
    /// </summary>
    Task<JourneyPolylineResponse?> GetNearestWaypointPolylineAsync(
        Guid journeyId,
        Guid travelerId,
        double currentLatitude,
        double currentLongitude,
        bool excludeCompletedWaypoints = true,
        CancellationToken cancellationToken = default);
}
