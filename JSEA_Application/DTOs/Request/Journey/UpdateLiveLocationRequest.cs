using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

/// <summary>Body POST /api/journeys/{id}/live-location — GPS realtime gửi định kỳ (1–3s).</summary>
public class UpdateLiveLocationRequest
{
    [Range(-90, 90, ErrorMessage = "latitude phải từ -90 đến 90")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "longitude phải từ -180 đến 180")]
    public double Longitude { get; set; }

    /// <summary>Sai số GPS (m), tuỳ chọn.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>Hướng di chuyển (0–360 độ), tuỳ chọn.</summary>
    public double? HeadingDegrees { get; set; }

    /// <summary>Khách (Kahoot-style): bắt buộc nếu không có JWT.</summary>
    public Guid? GuestKey { get; set; }
}
