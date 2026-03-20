using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

public class SaveWaypointsRequest
{
    [Required]
    public Guid SegmentId { get; set; }

    [Required]
    public List<SaveWaypointItemRequest> Waypoints { get; set; } = new();
}

public class SaveWaypointItemRequest
{
    [Required]
    public Guid SuggestionId { get; set; }

    [Range(1, 100)]
    public int StopOrder { get; set; }
}
