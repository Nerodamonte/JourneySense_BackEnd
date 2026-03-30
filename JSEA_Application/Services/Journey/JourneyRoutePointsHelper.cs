using JSEA_Application.DTOs.Respone.Journey;
using JourneyEntity = JSEA_Application.Models.Journey;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Services.Journey;

/// <summary>Chuyển tuyến chính journey (LineString) hoặc fallback đoạn thẳng origin→destination cho FE vẽ map.</summary>
public static class JourneyRoutePointsHelper
{
    public static List<GeoPointResponse>? FromLineString(LineString? path)
    {
        if (path?.Coordinates is not { Length: >= 2 } coords)
            return null;
        return coords
            .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
            .ToList();
    }

    /// <summary>Tuyến primary lúc setup (segment_order nhỏ nhất), vẫn nằm trong route_segments dù journeys.route_path đã bị ghi đè.</summary>
    public static List<GeoPointResponse>? SetupPrimaryRouteFromSegments(JourneyEntity? j)
    {
        if (j?.RouteSegments == null || j.RouteSegments.Count == 0)
            return null;
        var seg = j.RouteSegments.OrderBy(s => s.SegmentOrder ?? int.MaxValue).FirstOrDefault();
        return FromLineString(seg?.SegmentPath);
    }

    public static List<GeoPointResponse>? FromRouteContext(RouteContext? ctx)
    {
        if (ctx?.RoutePath?.Coordinates is not { Length: >= 2 } coords)
            return null;
        return coords
            .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
            .ToList();
    }

    public static List<GeoPointResponse>? FromJourney(JourneyEntity? j)
    {
        if (j == null)
            return null;

        var fromPath = FromLineString(j.RoutePath);
        if (fromPath != null)
            return fromPath;

        if (j.OriginLocation != null && j.DestinationLocation != null)
        {
            return new List<GeoPointResponse>
            {
                new() { Latitude = j.OriginLocation.Y, Longitude = j.OriginLocation.X },
                new() { Latitude = j.DestinationLocation.Y, Longitude = j.DestinationLocation.X }
            };
        }

        return null;
    }
}
