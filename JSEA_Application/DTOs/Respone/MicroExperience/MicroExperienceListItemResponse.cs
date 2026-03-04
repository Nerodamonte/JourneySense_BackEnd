namespace JSEA_Application.DTOs.Respone.MicroExperience;

/// <summary>
/// Item dùng cho danh sách Experience (để staff xem/quản lý hoặc hiển thị list).
/// </summary>
public class MicroExperienceListItemResponse
{
    /// <summary>Id Experience.</summary>
    public Guid Id { get; set; }

    /// <summary>Tên Experience.</summary>
    public string? Name { get; set; }

    /// <summary>Thành phố.</summary>
    public string? City { get; set; }

    /// <summary>Trạng thái Experience (active/inactive...).</summary>
    public string? Status { get; set; }

    /// <summary>Các khung giờ phù hợp.</summary>
    public List<string>? PreferredTimes { get; set; }

    /// <summary>Vĩ độ (WGS84) để vẽ trên bản đồ.</summary>
    public double? Latitude { get; set; }

    /// <summary>Kinh độ (WGS84) để vẽ trên bản đồ.</summary>
    public double? Longitude { get; set; }
}
