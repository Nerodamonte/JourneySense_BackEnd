using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.MicroExperience;

public class CreateMicroExperienceRequest : IValidatableObject
{
    [Required(ErrorMessage = "Tên trải nghiệm không được để trống")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Danh mục không được để trống")]
    public Guid CategoryId { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    /// <summary>Vĩ độ WGS84. Gửi cùng <see cref="Longitude"/> để đặt điểm trên bản đồ; nếu bỏ trống cả hai thì geocode từ địa chỉ (Goong).</summary>
    [Range(-90, 90, ErrorMessage = "latitude từ -90 đến 90")]
    public double? Latitude { get; set; }

    /// <summary>Kinh độ WGS84. Đi kèm <see cref="Latitude"/>.</summary>
    [Range(-180, 180, ErrorMessage = "longitude từ -180 đến 180")]
    public double? Longitude { get; set; }

    /// <summary>Phương tiện có thể tiếp cận: walking, bicycle, motorbike, car.</summary>
    [Required(ErrorMessage = "Phương tiện tiếp cận không được để trống")]
    public List<string> AccessibleBy { get; set; } = new();

    /// <summary>Khung giờ phù hợp (Morning, Afternoon, Evening, Night).</summary>
    public List<string>? PreferredTimes { get; set; }

    /// <summary>Thời tiết phù hợp (Sunny, Cloudy, Rainy).</summary>
    public List<string>? WeatherSuitability { get; set; }

    /// <summary>Mùa phù hợp.</summary>
    public List<string>? Seasonality { get; set; }

    /// <summary>Tag tiện ích (amenity) cho experience (vd: parking, wifi, restroom).</summary>
    public List<string>? AmenityTags { get; set; }

    /// <summary>Tag vibe / cảm xúc (Chill, Explorer, ...) phục vụ embedding và gợi ý.</summary>
    public List<string>? Tags { get; set; }

    /// <summary>Mô tả phong phú cho AI embedding (100–200 chữ khuyến nghị).</summary>
    public string? RichDescription { get; set; }

    /// <summary>JSON giờ mở cửa (theo format app đang dùng).</summary>
    public string? OpeningHours { get; set; }

    public string? PriceRange { get; set; }

    /// <summary>Crowd: quiet, normal, hoặc busy.</summary>
    public string? CrowdLevel { get; set; }

    /// <summary>Tuỳ chọn: gắn ảnh theo URL (sau khi tạo entity sẽ lưu vào experience_photos).</summary>
    public List<ExperiencePhotoInput>? Photos { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude.HasValue ^ Longitude.HasValue)
        {
            yield return new ValidationResult(
                "Gửi đủ latitude và longitude, hoặc bỏ cả hai để geocode từ địa chỉ.",
                [nameof(Latitude), nameof(Longitude)]);
        }
    }
}
