using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Payment;

public class RedeemPackageByPointsRequest
{
    [Required]
    public Guid PackageId { get; set; }
}
