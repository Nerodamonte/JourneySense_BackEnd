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
}
