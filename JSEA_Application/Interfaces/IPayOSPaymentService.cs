using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.DTOs.Respone.Payment;

namespace JSEA_Application.Interfaces;

public interface IPayOSPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    Task<PaymentLinkInfoResponse?> GetPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default);

    Task<PaymentLinkInfoResponse?> CancelPaymentLinkAsync(string paymentLinkId, string? cancellationReason = null, CancellationToken cancellationToken = default);
}
