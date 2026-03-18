namespace JSEA_Application.DTOs.Respone.Payment;

public class ConfirmPaymentResponse
{
    public Guid TransactionId { get; set; }
    public string TransactionStatus { get; set; } = null!;
    public Guid? UserPackageId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
