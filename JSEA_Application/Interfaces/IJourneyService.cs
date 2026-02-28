using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;

namespace JSEA_Application.Interfaces;

public interface IJourneyService
{
    /// <summary>
    /// Validate request, gọi Goong phân tích tuyến, lưu Journey + waypoints, trả về response.
    /// </summary>
    Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default);
}
