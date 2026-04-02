namespace JSEA_Application.DTOs.Journey;

/// <summary>SignalR method <c>MemberLocationUpdated</c> (camelCase JSON). FE gọi hub <c>UpdateLocation</c> định kỳ khi journey đang chạy.</summary>
public class JourneyMemberLocationNotification
{
    public Guid JourneyId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? TravelerId { get; set; }
    public Guid? GuestKey { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? HeadingDegrees { get; set; }
    public DateTime AtUtc { get; set; }
}
