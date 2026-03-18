using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.DTOs.Respone.Payment;
using JSEA_Application.Interfaces;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace JSEA_Presentation.Services;

public class PayOSPaymentService : IPayOSPaymentService
{
    private readonly PayOSClient _client;

    public PayOSPaymentService(IOptions<PayOSOptions> options)
    {
        _client = new PayOSClient(options.Value);
    }

    public async Task<CreatePaymentResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var orderCode = request.OrderCode ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var returnUrl = request.ReturnUrl ?? "https://localhost:5001/payment/success";
        var cancelUrl = request.CancelUrl ?? "https://localhost:5001/payment/cancel";

        var payOsRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = request.TotalAmount,
            Description = request.Description ?? $"Đơn hàng {orderCode}",
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl,
            BuyerName = request.BuyerName,
            BuyerEmail = request.BuyerEmail,
            BuyerPhone = request.BuyerPhone,
            Items =
            [
                .. request.Items.Select(i => new PaymentLinkItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Unit = i.Unit
                })
            ]
        };

        var paymentLink = await _client.PaymentRequests.CreateAsync(payOsRequest);

        return new CreatePaymentResponse
        {
            OrderCode = orderCode,
            PaymentLinkId = paymentLink.PaymentLinkId,
            CheckoutUrl = paymentLink.CheckoutUrl,
            QrCode = paymentLink.QrCode,
            Status = paymentLink.Status.ToString(),
            Amount = paymentLink.Amount,
            Bin = paymentLink.Bin,
            AccountNumber = paymentLink.AccountNumber,
            AccountName = paymentLink.AccountName,
            Currency = paymentLink.Currency,
            Description = paymentLink.Description
        };
    }

    public async Task<PaymentLinkInfoResponse?> GetPaymentLinkAsync(string paymentLinkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentLink = await _client.PaymentRequests.GetAsync(paymentLinkId);
            return new PaymentLinkInfoResponse
            {
                PaymentLinkId = paymentLinkId,
                Status = paymentLink.Status.ToString(),
                Amount = paymentLink.Amount,
                AmountPaid = paymentLink.AmountPaid,
                AmountRemaining = paymentLink.AmountRemaining,
                Description = null,
                CanceledAt = paymentLink.CanceledAt,
                CancellationReason = paymentLink.CancellationReason
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<PaymentLinkInfoResponse?> CancelPaymentLinkAsync(string paymentLinkId, string? cancellationReason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentLink = await _client.PaymentRequests.CancelAsync(paymentLinkId, cancellationReason ?? "Hủy bởi người dùng");
            return new PaymentLinkInfoResponse
            {
                PaymentLinkId = paymentLinkId,
                Status = paymentLink.Status.ToString(),
                Amount = paymentLink.Amount,
                AmountPaid = paymentLink.AmountPaid,
                AmountRemaining = paymentLink.AmountRemaining,
                Description = null,
                CanceledAt = paymentLink.CanceledAt,
                CancellationReason = paymentLink.CancellationReason
            };
        }
        catch
        {
            return null;
        }
    }
}
