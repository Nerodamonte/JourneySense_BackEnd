using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.DTOs.Respone.Payment;

namespace JSEA_Application.Interfaces;

public interface IPurchaseService
{
    Task<PurchasePackageResponse> CreatePurchaseAsync(PurchasePackageRequest request, CancellationToken cancellationToken = default);
    Task<ConfirmPaymentResponse> ConfirmPaymentAsync(long orderCode, CancellationToken cancellationToken = default);
}
