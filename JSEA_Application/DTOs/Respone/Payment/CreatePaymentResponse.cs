namespace JSEA_Application.DTOs.Respone.Payment;

public class CreatePaymentResponse
{
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string? QrCode { get; set; }
    public string? Status { get; set; }
    public long Amount { get; set; }
    public string? Bin { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
}
