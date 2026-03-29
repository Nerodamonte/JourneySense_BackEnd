using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.MicroExperience;

/// <summary>Một ảnh gắn vào experience (URL sẵn — CDN hoặc link ngoài).</summary>
public class ExperiencePhotoInput
{
    [Required(ErrorMessage = "photoUrl không được để trống")]
    [StringLength(500)]
    public string PhotoUrl { get; set; } = null!;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string? Caption { get; set; }

    /// <summary>Ảnh bìa — nếu true, các ảnh khác của experience sẽ bỏ cờ cover.</summary>
    public bool IsCover { get; set; }
}
