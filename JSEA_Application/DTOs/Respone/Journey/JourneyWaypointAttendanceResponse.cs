namespace JSEA_Application.DTOs.Respone.Journey;

public class JourneyWaypointAttendanceResponse
{
    public Guid JourneyId { get; set; }
    public int ActiveMemberCount { get; set; }
    public List<JourneyWaypointAttendanceItemResponse> Waypoints { get; set; } = new();
}

public class JourneyWaypointAttendanceItemResponse
{
    public Guid WaypointId { get; set; }
    public int StopOrder { get; set; }

    /// <summary>Số thành viên đang active đã tới waypoint (check-in/out hoặc skip).</summary>
    public int ArrivedCount { get; set; }
}
