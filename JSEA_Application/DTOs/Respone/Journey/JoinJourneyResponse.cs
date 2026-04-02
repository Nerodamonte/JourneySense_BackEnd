namespace JSEA_Application.DTOs.Respone.Journey;

public class JoinJourneyResponse
{
    public Guid JourneyId { get; set; }
    public Guid MemberId { get; set; }
    public string Role { get; set; } = null!;
    public string DisplayName { get; set; } = null!;

    /// <summary>Chỉ khách; client lưu để gọi API guest.</summary>
    public Guid? GuestKey { get; set; }
}
