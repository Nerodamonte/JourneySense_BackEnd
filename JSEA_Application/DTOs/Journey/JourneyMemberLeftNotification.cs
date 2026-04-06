namespace JSEA_Application.DTOs.Journey;

/// <summary>SignalR: <c>MemberLeft</c> — member/guest đã leave (owner không leave qua API này).</summary>
public class JourneyMemberLeftNotification
{
    public Guid JourneyId { get; set; }

    public Guid MemberId { get; set; }
}
