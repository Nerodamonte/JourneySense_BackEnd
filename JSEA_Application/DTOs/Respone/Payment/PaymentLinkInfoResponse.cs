namespace JSEA_Application.DTOs.Respone.Payment;

public class PaymentLinkInfoResponse
{
    public string PaymentLinkId { get; set; } = null!;
    public string? Status { get; set; }
    public long Amount { get; set; }
    public long AmountPaid { get; set; }
    public long AmountRemaining { get; set; }
    public string? Description { get; set; }
    public string? CanceledAt { get; set; }
    public string? CancellationReason { get; set; }
}
