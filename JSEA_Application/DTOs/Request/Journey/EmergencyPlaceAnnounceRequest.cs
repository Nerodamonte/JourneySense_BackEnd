using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

/// <summary>FE gọi sau khi user chọn 1 địa điểm từ list (restaurant / lodging / coffee) để broadcast SignalR tới journey.</summary>
public class EmergencyPlaceAnnounceRequest
{
    [Required]
    public Guid JourneyId { get; set; }

    /// <summary>Khách: bắt buộc nếu không có JWT.</summary>
    public Guid? GuestKey { get; set; }

    /// <summary>Cùng tập <c>type</c> như <see cref="EmergencyNearbyRequest.Type"/>.</summary>
    [Required(ErrorMessage = "type là bắt buộc")]
    [StringLength(20)]
    public string Type { get; set; } = null!;

    [Required(ErrorMessage = "placeId là bắt buộc")]
    [StringLength(256)]
    public string PlaceId { get; set; } = null!;
}
