namespace JSEA_Application.DTOs.Respone.Place;

/// <summary>Chi tiết địa điểm từ Goong Place/Detail (sau khi chọn từ AutoComplete).</summary>
public class PlaceDetailResponse
{
    /// <summary>place_id từ Goong.</summary>
    public string PlaceId { get; set; } = null!;

    /// <summary>Tên địa điểm.</summary>
    public string? Name { get; set; }

    /// <summary>Địa chỉ đầy đủ đã format.</summary>
    public string? FormattedAddress { get; set; }

    /// <summary>Vĩ độ (WGS84).</summary>
    public double? Latitude { get; set; }

    /// <summary>Kinh độ (WGS84).</summary>
    public double? Longitude { get; set; }

    public string? FormattedPhoneNumber { get; set; }

    public string? InternationalPhoneNumber { get; set; }

    public double? Rating { get; set; }

    public int? UserRatingsTotal { get; set; }

    /// <summary>Giờ mở cửa (text), nếu Goong trả weekday_text hoặc open_now.</summary>
    public string? OpeningHoursSummary { get; set; }

    public bool? OpenNow { get; set; }
}
