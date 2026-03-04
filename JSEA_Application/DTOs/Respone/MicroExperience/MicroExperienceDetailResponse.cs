namespace JSEA_Application.DTOs.Respone.MicroExperience;

/// <summary>
/// Thông tin chi tiết một Experience để staff xem hoặc dùng cho màn chi tiết trên app.
/// </summary>
public class MicroExperienceDetailResponse
{
    /// <summary>Id Experience.</summary>
    public Guid Id { get; set; }

    /// <summary>Tên Experience.</summary>
    public string? Name { get; set; }

    /// <summary>Tên Category (nếu có).</summary>
    public string? CategoryName { get; set; }

    /// <summary>Mô tả chi tiết Experience.</summary>
    public string? Description { get; set; }

    /// <summary>Điểm rating trung bình (1–5).</summary>
    public decimal AvgRating { get; set; }

    /// <summary>Trạng thái Experience.</summary>
    public string? Status { get; set; }

    /// <summary>Địa chỉ hiển thị.</summary>
    public string? Address { get; set; }

    /// <summary>Thành phố.</summary>
    public string? City { get; set; }

    /// <summary>Quốc gia (mặc định Vietnam).</summary>
    public string? Country { get; set; }

    /// <summary>Các phương tiện có thể tiếp cận (walking, bicycle, motorbike, car...).</summary>
    public List<string>? AccessibleBy { get; set; }

    /// <summary>Các khung giờ phù hợp (Morning, Afternoon, Evening, Night...).</summary>
    public List<string>? PreferredTimes { get; set; }

    /// <summary>Thời tiết phù hợp (Sunny, Cloudy, Rainy...).</summary>
    public List<string>? WeatherSuitability { get; set; }

    /// <summary>Mùa phù hợp.</summary>
    public List<string>? Seasonality { get; set; }

    /// <summary>Tên các factor (vibe/mood) được tag (từ bảng factors).</summary>
    public List<string>? FactorNames { get; set; }

    /// <summary>Vĩ độ (WGS84).</summary>
    public double? Latitude { get; set; }

    /// <summary>Kinh độ (WGS84).</summary>
    public double? Longitude { get; set; }
}
