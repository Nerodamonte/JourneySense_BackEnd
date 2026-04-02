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
    /// Lưu danh sách các điểm user chọn ghé theo segment đã chọn. Kiểm tra quãng segment với hạn km còn lại của gói.
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

    /// <summary>Member khách (guest_key): cùng logic polyline toàn tuyến như user đăng nhập.</summary>
    Task<JourneyPolylineResponse?> GetJourneyPolylineForGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);

    /// <summary>Member khách: tuyến từ GPS hiện tại tới waypoint kế tiếp (theo tiến độ của chính khách).</summary>
    Task<JourneyPolylineResponse?> GetNearestWaypointPolylineForGuestAsync(
        Guid journeyId,
        Guid guestKey,
        double currentLatitude,
        double currentLongitude,
        bool excludeCompletedWaypoints = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm x/N từng waypoint (N = thành viên active). Owner hoặc member đã join.
    /// </summary>
    Task<JourneyWaypointAttendanceResponse?> GetWaypointAttendanceAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    /// <summary>Giống <see cref="GetWaypointAttendanceAsync"/> cho khách đã join bằng guest_key.</summary>
    Task<JourneyWaypointAttendanceResponse?> GetWaypointAttendanceForGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);

    /// <summary>Chủ hoặc member active; nếu không thấy journey hoặc không có quyền → throw.</summary>
    Task VerifyTravelerCanNavigateJourneyAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    /// <summary>Member khách active trên journey; throw nếu không hợp lệ.</summary>
    Task VerifyGuestCanNavigateJourneyAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);

    /// <summary>Giống <see cref="VerifyTravelerCanNavigateJourneyAsync"/> và bắt buộc journey đã start (cho SignalR).</summary>
    Task VerifyTravelerCanNavigateStartedJourneyAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    /// <summary>Giống <see cref="VerifyTravelerCanNavigateStartedJourneyAsync"/> cho khách.</summary>
    Task VerifyGuestCanNavigateStartedJourneyAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);
}
