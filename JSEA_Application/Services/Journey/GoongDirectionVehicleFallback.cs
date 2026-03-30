using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Services.Journey;

/// <summary>
/// Cùng cách xử lý emergency từ đầu (repair / hospital / pharmacy): Goong chỉ dùng <c>car</c> vs <c>bike</c>,
/// thử phương tiện user chọn rồi mode còn lại (motorbike/bicycle/walking → bike).
/// </summary>
public static class GoongDirectionVehicleFallback
{
    public static async Task<RouteContext?> GetDirectionFirstSuccessfulAsync(
        IGoongMapsService goongMaps,
        Point origin,
        Point destination,
        VehicleType primary,
        List<Point>? waypoints,
        CancellationToken cancellationToken = default)
    {
        foreach (var gv in GoongVehicleFallbackChain(primary))
        {
            var route = await goongMaps.GetDirectionRouteWithGoongVehicleAsync(
                origin, destination, gv, waypoints, cancellationToken);
            if (route != null)
                return route;
        }

        return null;
    }

    /// <summary>Chỉ car + bike — đúng với luồng 3 type đầu, tránh spam nhiều vehicle Goong.</summary>
    public static IReadOnlyList<string> GoongVehicleFallbackChain(VehicleType primary)
    {
        if (primary == VehicleType.Car)
            return new[] { "car", "bike" };

        return new[] { "bike", "car" };
    }
}
