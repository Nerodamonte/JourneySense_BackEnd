using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Package;

public class CreatePackageDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; set; }

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int DistanceLimitKm { get; set; }

    public string? Benefit { get; set; }

    public bool? IsPopular { get; set; }

    public bool IsActive { get; set; } = true;
}

