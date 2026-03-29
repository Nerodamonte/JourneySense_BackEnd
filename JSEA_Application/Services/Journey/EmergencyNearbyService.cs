using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Services.Journey;

public class EmergencyNearbyService : IEmergencyNearbyService
{
    private readonly IGoongMapsService _goongMaps;

    /// <summary>Mặc định: ~8 km — đủ “quanh đây”, không kéo BV tỉnh xa.</summary>
    private const int DefaultRadiusMeters = 8000;

    /// <summary>Trần server: áp dụng dù client gửi 50km, tránh list 20–50 km như tìm kiếm toàn miền.</summary>
    private const int MaxRadiusMeters = 20000;

    public EmergencyNearbyService(IGoongMapsService goongMaps)
    {
        _goongMaps = goongMaps;
    }

    public async Task<(int StatusCode, string? ErrorMessage, IReadOnlyList<EmergencyNearbyItemResponse> Items)> GetNearbyAsync(
        EmergencyNearbyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeType(request.Type, out var placeType, out var typeError))
            return (400, typeError, Array.Empty<EmergencyNearbyItemResponse>());

        if (!IsUsableGpsForNearby(request.Latitude, request.Longitude, out var coordError))
            return (400, coordError, Array.Empty<EmergencyNearbyItemResponse>());

        var rawRadius = request.RadiusMeters is > 0 ? request.RadiusMeters!.Value : DefaultRadiusMeters;
        var userRadius = Math.Clamp(rawRadius, 100, MaxRadiusMeters);
        var maxResults = request.MaxResults is > 0 ? request.MaxResults!.Value : 1;
        maxResults = Math.Min(maxResults, 10);

        var searchInput = ResolveSearchInput(placeType, request.PlaceKeyword, request.VehicleType);
        // Luôn xin đủ prediction (≤20) để khi chỉ trả 1 điểm vẫn là gần nhất trong batch Goong, không cắt còn 6 suggestions.
        var autocompleteLimit = Math.Min(20, Math.Max(15, Math.Max(maxResults * 4, maxResults + 5)));

        // Bias Goong: hơi lớn hơn userRadius để autocomplete không bỏ sót POI “đường vòng”, nhưng không mở 50km.
        var placesBiasRadius = Math.Min(MaxRadiusMeters + 5000, Math.Max(userRadius + 2500, 4000));
        var crowCapMeters = Math.Min(MaxRadiusMeters + 5000, (int)Math.Ceiling(userRadius * 1.25));

        var suggestions = await _goongMaps.SearchPlaceSuggestionsAsync(
            searchInput,
            request.Latitude,
            request.Longitude,
            autocompleteLimit,
            placesBiasRadius,
            cancellationToken);

        if (suggestions.Count == 0)
            return (200, null, Array.Empty<EmergencyNearbyItemResponse>());

        var vehicle = ResolveVehicleForDirections(request.VehicleType);
        var origin = SnapPoint(request.Longitude, request.Latitude);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<EmergencyNearbyItemResponse>();

        foreach (var s in suggestions)
        {
            if (!seen.Add(s.PlaceId))
                continue;

            var detail = await _goongMaps.GetPlaceDetailAsync(s.PlaceId, cancellationToken);
            if (detail?.Latitude == null || detail.Longitude == null)
                continue;

            var crow = DistanceMeters(
                request.Latitude,
                request.Longitude,
                detail.Latitude.Value,
                detail.Longitude.Value);
            var crowRounded = Math.Round(crow, MidpointRounding.AwayFromZero);
            if (crowRounded > crowCapMeters)
                continue;

            var dest = SnapPoint(detail.Longitude.Value, detail.Latitude.Value);
            // Goong Direction đôi khi không trả routes cho một vehicle; thử lần lượt car/motorbike/...
            var route = await GetDirectionFirstSuccessfulAsync(origin, dest, vehicle, cancellationToken);

            double distanceDisplay;
            int? durationMin = null;
            var fallback = false;
            if (route != null)
            {
                distanceDisplay = route.TotalDistanceMeters;
                durationMin = route.EstimatedDurationMinutes;
            }
            else
            {
                distanceDisplay = crowRounded;
                fallback = true;
            }

            var distRounded = Math.Round(distanceDisplay, MidpointRounding.AwayFromZero);
            if (distRounded > userRadius)
                continue;

            rows.Add(new EmergencyNearbyItemResponse
            {
                PlaceId = detail.PlaceId,
                Type = placeType,
                Name = detail.Name,
                FormattedAddress = detail.FormattedAddress,
                Phone = detail.FormattedPhoneNumber ?? detail.InternationalPhoneNumber,
                Latitude = detail.Latitude,
                Longitude = detail.Longitude,
                DistanceMeters = distRounded,
                UsedStraightLineFallback = fallback,
                RoutePolyline = route?.Polyline,
                EstimatedDurationMinutes = durationMin,
                Rating = detail.Rating,
                UserRatingsTotal = detail.UserRatingsTotal,
                OpeningHoursSummary = detail.OpeningHoursSummary,
                OpenNow = detail.OpenNow
            });

        }

        var ordered = rows
            .OrderBy(r => r.DistanceMeters)
            .Take(maxResults)
            .ToList();

        return (200, null, ordered);
    }

    private async Task<RouteContext?> GetDirectionFirstSuccessfulAsync(
        Point origin,
        Point dest,
        VehicleType primary,
        CancellationToken cancellationToken)
    {
        foreach (var v in VehicleFallbackChain(primary))
        {
            var route = await _goongMaps.GetDirectionRouteAsync(origin, dest, v, null, cancellationToken);
            if (route != null)
                return route;
        }

        return null;
    }

    /// <summary>Thứ tự ưu tiên: xe user chọn → motorbike → car → bicycle → walking (Goong map walking→bike).</summary>
    private static IEnumerable<VehicleType> VehicleFallbackChain(VehicleType primary)
    {
        yield return primary;
        foreach (var v in new[] { VehicleType.Motorbike, VehicleType.Car, VehicleType.Bicycle, VehicleType.Walking })
        {
            if (v != primary)
                yield return v;
        }
    }

    private static Point SnapPoint(double longitude, double latitude) =>
        new(Math.Round(longitude, 6), Math.Round(latitude, 6)) { SRID = 4326 };

    /// <summary>Swagger/OpenAPI hay để mặc định "string" — không được dùng làm từ khóa Goong.</summary>
    private static string ResolveSearchInput(string placeType, string? placeKeyword, string? vehicleType)
    {
        var trimmed = placeKeyword?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return EmergencyPlaceTypes.SearchInputFor(placeType, vehicleType);
        if (trimmed.Equals("string", StringComparison.OrdinalIgnoreCase))
            return EmergencyPlaceTypes.SearchInputFor(placeType, vehicleType);
        return trimmed;
    }

    private static VehicleType ResolveVehicleForDirections(string? vehicleTypeLower)
    {
        if (!string.IsNullOrWhiteSpace(vehicleTypeLower))
        {
            return vehicleTypeLower.Trim().ToLowerInvariant() switch
            {
                "car" => VehicleType.Car,
                "motorbike" => VehicleType.Motorbike,
                "bicycle" => VehicleType.Bicycle,
                "walking" => VehicleType.Walking,
                _ => VehicleType.Motorbike
            };
        }

        // Một nút khẩn cấp — không hỏi phương tiện: mặc định xe máy (VN). Gửi vehicleType nếu cần ô tô/đi bộ.
        return VehicleType.Motorbike;
    }

    /// <summary>GPS thật; 90/180 hay (0,0) thường là nhập thử Swagger — mọi BV đều xa &gt; bán kính → [] khó hiểu.</summary>
    private static bool IsUsableGpsForNearby(double lat, double lon, out string? error)
    {
        error = null;
        if (double.IsNaN(lat) || double.IsInfinity(lat) || double.IsNaN(lon) || double.IsInfinity(lon))
        {
            error = "latitude/longitude không hợp lệ.";
            return false;
        }

        if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
        {
            error = "latitude/longitude nằm ngoài phạm vi Trái Đất hợp lệ.";
            return false;
        }

        if (Math.Abs(lat) < 1e-9 && Math.Abs(lon) < 1e-9)
        {
            error = "Tọa độ (0, 0) không dùng được cho tìm gần đây — lấy GPS từ thiết bị.";
            return false;
        }

        if (Math.Abs(lat) >= 89.9 || Math.Abs(lon) >= 179.9)
        {
            error = "Tọa độ sát cực hoặc biên kinh tuyến (ví dụ 90, 180) không phải vị trí người dùng — dùng GPS thật (ở VN thường ~8–24°N, ~102–110°E).";
            return false;
        }

        return true;
    }

    private static bool TryNormalizeType(string? type, out string placeType, out string? error)
    {
        placeType = "";
        error = null;
        if (string.IsNullOrWhiteSpace(type))
        {
            error = "type không được để trống.";
            return false;
        }

        var t = EmergencyPlaceTypes.NormalizeOrNull(type);
        if (t != null)
        {
            placeType = t;
            return true;
        }

        error = "type phải là repair_shop, hospital hoặc pharmacy.";
        return false;
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000;
        var dLat = (lat2 - lat1) * (Math.PI / 180);
        var dLon = (lon2 - lon1) * (Math.PI / 180);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }
}
