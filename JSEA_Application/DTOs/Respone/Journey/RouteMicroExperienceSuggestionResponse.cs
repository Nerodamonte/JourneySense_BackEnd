namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Một Experience được gợi ý dọc/gần tuyến của journey, kèm thông tin định vị và thời gian dừng ước tính.
/// </summary>
public class RouteMicroExperienceSuggestionResponse
{
    /// <summary>Id Experience.</summary>
    public Guid Id { get; set; }

    /// <summary>Tên Experience.</summary>
    public string? Name { get; set; }

    /// <summary>Tên danh mục (Category) nếu có.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Địa chỉ hiển thị cho Experience.</summary>
    public string? Address { get; set; }

    /// <summary>Thành phố.</summary>
    public string? City { get; set; }

    /// <summary>Quốc gia (mặc định Vietnam).</summary>
    public string? Country { get; set; }

    /// <summary>Các khung giờ gợi ý (Morning, Afternoon, Evening, Night...).</summary>
    public List<string>? PreferredTimes { get; set; }

    /// <summary>Trạng thái Experience (active/inactive...).</summary>
    public string? Status { get; set; }

    /// <summary>Vĩ độ (WGS84) của Experience.</summary>
    public double? Latitude { get; set; }

    /// <summary>Kinh độ (WGS84) của Experience.</summary>
    public double? Longitude { get; set; }

    /// <summary>Khoảng cách lệch (mét) từ tuyến chính đến Experience.</summary>
    public int DetourDistanceMeters { get; set; }

    /// <summary>Thời gian dừng ước tính tại Experience (phút).</summary>
    public int EstimatedStopMinutes { get; set; }
}
