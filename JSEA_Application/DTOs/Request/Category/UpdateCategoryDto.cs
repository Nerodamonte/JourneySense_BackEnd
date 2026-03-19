using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Category;

public class UpdateCategoryDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

