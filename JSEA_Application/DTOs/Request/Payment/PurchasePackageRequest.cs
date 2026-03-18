using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Payment;

public class PurchasePackageRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PackageId { get; set; }

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }
}
