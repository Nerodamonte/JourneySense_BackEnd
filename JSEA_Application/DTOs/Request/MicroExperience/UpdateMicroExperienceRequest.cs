using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.MicroExperience;

public class UpdateMicroExperienceRequest
{
    [Required(ErrorMessage = "Tên trải nghiệm không được để trống")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Danh mục không được để trống")]
    public Guid CategoryId { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public ExperienceStatus Status { get; set; }
}
