using System.Text.Json;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace JSEA_Infrastructure.Services.Goong;

public class GoongMapsService : IGoongMapsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoongOptions _options;
    private const string DefaultHost = "https://rsapi.goong.io";

    public GoongMapsService(IHttpClientFactory httpClientFactory, IOptions<GoongOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    private string GetBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            return _options.BaseUrl.TrimEnd('/') + "/";
        return $"{DefaultHost}/";
    }

    public async Task<List<RouteContext>> AnalyzeRouteContextAsync(
        string originAddress,
        string destinationAddress,
        VehicleType vehicleType,
        int timeBudgetMinutes,
        int maxDetourDistanceMeters,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var key = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            return new List<RouteContext>();

        // 1. Geocode origin
        var originLoc = await GeocodeAsync(client, key, originAddress, cancellationToken);
        if (originLoc == null) return new List<RouteContext>();

        // 2. Geocode destination
        var destLoc = await GeocodeAsync(client, key, destinationAddress, cancellationToken);
        if (destLoc == null) return new List<RouteContext>();

        // 3. Direction
        var baseUrl = GetBaseUrl();
        var vehicle = MapVehicle(vehicleType);
        var originStr = $"{originLoc.Value.lat},{originLoc.Value.lng}";
        var destStr = $"{destLoc.Value.lat},{destLoc.Value.lng}";
        // Nếu cấu hình cho phép nhiều route, bật alternatives=true để Goong trả về nhiều tuyến (nếu có).
        var alternatives = _options.MaxRouteAlternatives > 1 ? "true" : "false";
        var url = $"{baseUrl}Direction?origin={Uri.EscapeDataString(originStr)}&destination={Uri.EscapeDataString(destStr)}&vehicle={vehicle}&alternatives={alternatives}&api_key={key}";

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return new List<RouteContext>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var routes = root.GetProperty("routes");
        var routeCount = routes.GetArrayLength();
        if (routeCount == 0) return new List<RouteContext>();

        var maxRoutes = _options.MaxRouteAlternatives <= 0 ? 1 : _options.MaxRouteAlternatives;
        var take = Math.Min(routeCount, maxRoutes);

        var originPoint = new Point(originLoc.Value.lng, originLoc.Value.lat) { SRID = 4326 };
        var destPoint = new Point(destLoc.Value.lng, destLoc.Value.lat) { SRID = 4326 };

        var result = new List<RouteContext>(take);

        for (var i = 0; i < take; i++)
        {
            var route = routes[i];
            var legs = route.GetProperty("legs");
            if (legs.GetArrayLength() == 0)
                continue;

            var leg = legs[0];
            var distanceMeters = leg.GetProperty("distance").GetProperty("value").GetInt32();
            var durationSeconds = leg.GetProperty("duration").GetProperty("value").GetInt32();
            var durationMinutes = (int)Math.Ceiling(durationSeconds / 60.0);

            LineString? routePath = null;
            string? encoded = null;
            if (route.TryGetProperty("overview_polyline", out var polylineEl) &&
                polylineEl.TryGetProperty("points", out var pointsEl))
            {
                encoded = pointsEl.GetString();
                if (!string.IsNullOrEmpty(encoded))
                    routePath = DecodePolylineToLineString(encoded);
            }

            result.Add(new RouteContext
            {
                RoutePath = routePath,
                TotalDistanceMeters = distanceMeters,
                EstimatedDurationMinutes = durationMinutes,
                OriginLocation = originPoint,
                DestinationLocation = destPoint,
                OriginLatitude = originPoint.Y,
                OriginLongitude = originPoint.X,
                DestinationLatitude = destPoint.Y,
                DestinationLongitude = destPoint.X,
                Polyline = encoded
            });
        }

        return result;
    }

    public async Task<Point?> GeocodeAddressToPointAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var client = _httpClientFactory.CreateClient();
        var key = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var coord = await GeocodeAsync(client, key, address.Trim(), cancellationToken);
        if (coord == null)
            return null;

        return new Point(coord.Value.lng, coord.Value.lat) { SRID = 4326 };
    }

    private async Task<(double lat, double lng)?> GeocodeAsync(
        HttpClient client,
        string apiKey,
        string address,
        CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();
        var url = $"{baseUrl}Geocode?address={Uri.EscapeDataString(address)}&api_key={apiKey}";
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var first = results[0];
        var geom = first.GetProperty("geometry");
        var loc = geom.GetProperty("location");
        var lat = loc.GetProperty("lat").GetDouble();
        var lng = loc.GetProperty("lng").GetDouble();
        return (lat, lng);
    }

    private static string MapVehicle(VehicleType type)
    {
        return type switch
        {
            VehicleType.Car => "car",
            VehicleType.Bicycle => "bike",
            VehicleType.Motorbike => "bike",
            VehicleType.Walking => "bike",
            _ => "car"
        };
    }

    /// <summary>
    /// Decode Google/Goong encoded polyline to NTS LineString (SRID 4326).
    /// </summary>
    private static LineString? DecodePolylineToLineString(string encoded)
    {
        var coords = DecodePolyline(encoded);
        if (coords.Count == 0) return null;
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var line = factory.CreateLineString(coords.ToArray());
        return line;
    }

    private static List<Coordinate> DecodePolyline(string encoded)
    {
        var list = new List<Coordinate>();
        int i = 0;
        int lat = 0, lng = 0;
        while (i < encoded.Length)
        {
            int b, shift = 0, result = 0;
            do
            {
                b = encoded[i++] - 63;
                result |= (b & 31) << shift;
                shift += 5;
            } while (b >= 32);
            int dlat = (result & 1) != 0 ? ~(result >> 1) : (result >> 1);
            lat += dlat;

            shift = 0; result = 0;
            do
            {
                b = encoded[i++] - 63;
                result |= (b & 31) << shift;
                shift += 5;
            } while (b >= 32);
            int dlng = (result & 1) != 0 ? ~(result >> 1) : (result >> 1);
            lng += dlng;

            list.Add(new Coordinate(lng / 1e5, lat / 1e5));
        }
        return list;
    }
}
