using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Category;

public class CreateCategoryDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}