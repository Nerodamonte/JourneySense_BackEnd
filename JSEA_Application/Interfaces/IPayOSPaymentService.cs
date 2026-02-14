using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.DTOs.Respone.Payment;

namespace JSEA_Application.Interfaces;

public interface IPayOSPaymentService
{
    /// <summary>
    /// Tạo link thanh toán PayOS.
    /// </summary>
    Task<CreatePaymentResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin trạng thái link thanh toán theo PaymentLinkId.
    /// </summary>
    Task<PaymentLinkInfoResponse?> GetPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy link thanh toán.
    /// </summary>
    Task<PaymentLinkInfoResponse?> CancelPaymentLinkAsync(string paymentLinkId, string? cancellationReason = null, CancellationToken cancellationToken = default);
}
