namespace JSEA_Application.DTOs.Respone.Journey;

public class SavedWaypointItemResponse
{
    public Guid WaypointId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? SuggestionId { get; set; }
    public int StopOrder { get; set; }
}

public class SaveWaypointsResponse
{
    public string Message { get; set; } = "";
    public List<SavedWaypointItemResponse>? Waypoints { get; set; }
}
