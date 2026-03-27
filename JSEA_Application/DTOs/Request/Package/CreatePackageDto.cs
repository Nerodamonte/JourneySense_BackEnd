using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Package;

public class CreatePackageDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public PackageType Type { get; set; }

    [Range(0, int.MaxValue)]
    public int DistanceLimitKm { get; set; }

    [Range(0, int.MaxValue)]
    public int DurationInDays { get; set; }

    public string? Benefit { get; set; }

    public bool? IsPopular { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int? PointsRequired { get; set; }
}

