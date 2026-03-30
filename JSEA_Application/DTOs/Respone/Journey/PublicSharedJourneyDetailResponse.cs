using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

public class PublicSharedJourneyDetailResponse
{
    public string ShareCode { get; set; } = null!;

    public Guid JourneyId { get; set; }

    public string? TravelerName { get; set; }
    public string? TravelerAvatarUrl { get; set; }

    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }

    public VehicleType? VehicleType { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int ViewCount { get; set; }

    public string? JourneyFeedback { get; set; }

    /// <summary>Tuyến chính user đã chọn (journeys.route_path), sau khi lưu waypoint / chọn segment — dùng để replay chuyến.</summary>
    public List<GeoPointResponse>? RoutePoints { get; set; }

    /// <summary>Tuyến primary lúc setup (route_segments segment_order = 1), trước khi ghi đè route_path — O→D gợi ý ban đầu.</summary>
    public List<GeoPointResponse>? SetupPrimaryRoutePoints { get; set; }

    public List<PublicSharedJourneyWaypointResponse> Waypoints { get; set; } = new();
}

public class PublicSharedJourneyWaypointResponse
{
    public Guid WaypointId { get; set; }
    public int StopOrder { get; set; }

    public Guid ExperienceId { get; set; }
    public string? ExperienceName { get; set; }
    public string? ExperienceAddress { get; set; }

    public double? ExperienceLatitude { get; set; }
    public double? ExperienceLongitude { get; set; }

    public string? ExperienceDescription { get; set; }

    public List<string> ExperiencePhotoUrls { get; set; } = new();

    public DateTime? ActualArrivalAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }

    public int? RatingValue { get; set; }

    public string? FeedbackText { get; set; }
    public DateTime? FeedbackCreatedAt { get; set; }
}
