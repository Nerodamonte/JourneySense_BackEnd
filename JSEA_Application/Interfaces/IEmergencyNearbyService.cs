using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;

namespace JSEA_Application.Interfaces;

public interface IEmergencyNearbyService
{
    /// <summary>gọi Goong AutoComplete + Place Detail; 200 + items; 400/503.</summary>
    Task<(int StatusCode, string? ErrorMessage, IReadOnlyList<EmergencyNearbyItemResponse> Items)> GetNearbyAsync(
        EmergencyNearbyRequest request,
        CancellationToken cancellationToken = default);
}
