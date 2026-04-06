namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>Danh sách thành viên active (phòng chờ + đang đi) — poll trước/sau start.</summary>
public class JourneyMemberRosterItemResponse
{
    public Guid MemberId { get; set; }

    public string DisplayName { get; set; } = null!;

    /// <summary><c>owner</c> hoặc <c>member</c>.</summary>
    public string Role { get; set; } = null!;

    public bool IsGuest { get; set; }

    public DateTime JoinedAt { get; set; }

    /// <summary>Có giá trị nếu đã có vị trí trong cache (sau khi journey start và member gửi GPS).</summary>
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
