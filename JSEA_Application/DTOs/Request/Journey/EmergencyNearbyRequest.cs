using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

/// <summary>Body POST /api/emergency/nearby — một chạm khẩn cấp: chỉ cần type + GPS (Goong). Mặc định trả 1 địa điểm gần nhất.</summary>
public class EmergencyNearbyRequest
{
    /// <summary>
    /// repair_shop | hospital | pharmacy | gas_station | restaurant | lodging | coffee
    /// (xăng, ăn, nghỉ, cà phê — từ khóa Goong nội bộ).
    /// </summary>
    [Required(ErrorMessage = "type là bắt buộc")]
    [StringLength(20)]
    public string Type { get; set; } = null!;

    [Range(-90, 90, ErrorMessage = "latitude phải từ -90 đến 90")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "longitude phải từ -180 đến 180")]
    public double Longitude { get; set; }

    /// <summary>Chỉ giữ địa điểm có quãng đường &lt;= giá trị này (m). Mặc định 8000; server không vượt quá 20000 dù gửi cao hơn.</summary>
    [Range(100, 50000)]
    public int? RadiusMeters { get; set; }

    /// <summary>Nâng cao / debug — app khẩn cấp thường không gửi (để null).</summary>
    [StringLength(120)]
    public string? PlaceKeyword { get; set; }

    /// <summary>
    /// Số kết quả (đã sắp gần → xa). Mặc định theo <c>type</c>: bv/xăng/sửa xe/thuốc → 1; quán ăn/nhà nghỉ/cà phê → 5. Tối đa 10.
    /// </summary>
    [Range(1, 10)]
    public int? MaxResults { get; set; }

    /// <summary>Tuỳ chọn: walking | bicycle | motorbike | car. Mặc định motorbike nếu bỏ trống.</summary>
    [StringLength(20)]
    public string? VehicleType { get; set; }

    /// <summary>Nếu có, chỉ **owner hoặc member active** của journey này mới gọi được (JWT hoặc <see cref="GuestKey"/>).</summary>
    public Guid? JourneyId { get; set; }

    /// <summary>Khách (Kahoot-style): bắt buộc khi gọi không JWT và có <see cref="JourneyId"/>; phải trùng member active đã join.</summary>
    public Guid? GuestKey { get; set; }
}
