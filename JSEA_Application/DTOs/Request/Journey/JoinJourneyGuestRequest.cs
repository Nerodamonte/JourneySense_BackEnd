using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

public class JoinJourneyGuestRequest
{
    [Required(ErrorMessage = "displayName là bắt buộc")]
    [StringLength(120)]
    public string DisplayName { get; set; } = null!;

    /// <summary>Lần đầu bỏ trống; client gửi lại key đã nhận khi join lại cùng thiết bị.</summary>
    public Guid? GuestKey { get; set; }
}
