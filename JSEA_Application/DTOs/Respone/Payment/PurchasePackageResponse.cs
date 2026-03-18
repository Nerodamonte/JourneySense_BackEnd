namespace JSEA_Application.DTOs.Respone.Payment;

public class PurchasePackageResponse
{
    public Guid TransactionId { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = null!;
    public string? QrCode { get; set; }
    public string Status { get; set; } = null!;
}
