using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;

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
    /// Gợi ý micro-experiences dọc/gần tuyến theo journey: lọc theo vibe (current_mood), weather, timeOfDay, khoảng cách lệch, status. Sắp xếp theo khoảng cách.
    /// </summary>
    Task<List<RouteMicroExperienceSuggestionResponse>> GetSuggestionsAlongRouteAsync(Guid journeyId, int? limit, JSEA_Application.Enums.WeatherType? weather, JSEA_Application.Enums.TimeOfDay? timeOfDay, CancellationToken cancellationToken = default);
}
