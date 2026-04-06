namespace JSEA_Application.DTOs.Journey;

/// <summary>SignalR: <c>MemberJoined</c> — có thành viên mới (kể cả owner lần đầu vào roster).</summary>
public class JourneyMemberJoinedNotification
{
    public Guid JourneyId { get; set; }

    public Guid MemberId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsGuest { get; set; }
}
