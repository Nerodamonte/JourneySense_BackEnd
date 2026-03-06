namespace JSEA_Application.DTOs.Respone.Place;

/// <summary>Một gợi ý địa điểm từ Goong Place AutoComplete.</summary>
public class PlaceSuggestionResponse
{
    /// <summary>Địa chỉ đầy đủ hiển thị cho user.</summary>
    public string Description { get; set; } = null!;

    /// <summary>place_id từ Goong, dùng để gọi Place/Detail hoặc Geocode.</summary>
    public string PlaceId { get; set; } = null!;

    /// <summary>Dòng chính (tên địa điểm / số nhà).</summary>
    public string? MainText { get; set; }

    /// <summary>Dòng phụ (quận, thành phố).</summary>
    public string? SecondaryText { get; set; }

    /// <summary>Plus code (ví dụ +6DW1G Trung Hòa, Cầu Giấy, Hà Nội).</summary>
    public string? PlusCode { get; set; }
}
