namespace JSEA_Application.DTOs.Respone.Package;

public class PackageResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string Type { get; set; } = null!;
    public int DistanceLimitKm { get; set; }
    public string? Benefit { get; set; }
    public bool? IsPopular { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

