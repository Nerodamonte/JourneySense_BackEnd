using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;

namespace JSEA_Application.Interfaces;

public interface IGoongMapsService
{
    /// <summary>
    /// Phân tích tuyến từ địa chỉ đi → đến: geocode, direction, trả về RouteContext.
    /// </summary>
    Task<RouteContext?> AnalyzeRouteContextAsync(
        string originAddress,
        string destinationAddress,
        VehicleType vehicleType,
        int timeBudgetMinutes,
        int maxDetourDistanceMeters,
        CancellationToken cancellationToken = default);
}
