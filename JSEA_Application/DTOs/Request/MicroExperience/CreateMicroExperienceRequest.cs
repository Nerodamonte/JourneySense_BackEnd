using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.MicroExperience;

public class CreateMicroExperienceRequest
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
}
