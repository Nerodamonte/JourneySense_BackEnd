namespace JSEA_Application.DTOs.Respone.UserPackage;

public class UserCurrentPackageDto
{
    public Guid UserId { get; set; }
    public Guid UserPackageId { get; set; }
    public Guid PackageId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string Type { get; set; } = null!;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = null!;

    public int DistanceLimitKm { get; set; }
    public decimal UsedKm { get; set; }
    public int DurationInDays { get; set; }
}

