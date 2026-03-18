namespace JSEA_Application.DTOs.Respone.Journey;

public class GeoPointResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class RouteSegmentResponse
{
    public Guid SegmentId { get; set; }
    public int? SegmentOrder { get; set; }
    public int? DistanceMeters { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool? IsScenic { get; set; }
    public bool? IsBusy { get; set; }
    public bool? IsCulturalArea { get; set; }
}

public class JourneyWaypointResponse
{
    public Guid WaypointId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? SuggestionId { get; set; }
    public Guid? SegmentId { get; set; }

    public int StopOrder { get; set; }
    public int? PlannedStopMinutes { get; set; }

    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public int? DetourDistanceMeters { get; set; }
    public int? DetourTimeMinutes { get; set; }
}
