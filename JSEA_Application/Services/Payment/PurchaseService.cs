using System.Text.Json;
using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.DTOs.Respone.Payment;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using UserPackageModel = JSEA_Application.Models.UserPackage;

namespace JSEA_Application.Services.Payment;

public class PurchaseService : IPurchaseService
{
    private readonly IPackageRepository _packageRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserPackageRepository _userPackageRepository;
    private readonly IPayOSPaymentService _payOSPaymentService;

    public PurchaseService(
        IPackageRepository packageRepository,
        ITransactionRepository transactionRepository,
        IUserPackageRepository userPackageRepository,
        IPayOSPaymentService payOSPaymentService)
    {
        _packageRepository = packageRepository;
        _transactionRepository = transactionRepository;
        _userPackageRepository = userPackageRepository;
        _payOSPaymentService = payOSPaymentService;
    }

    public async Task<PurchasePackageResponse> CreatePurchaseAsync(
        PurchasePackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
        if (package == null || package.IsActive != true)
            throw new ArgumentException("Gói không tồn tại hoặc đã ngừng hoạt động.");

        var amount = (long)(package.SalePrice ?? package.Price);
        var nowUtc = DateTime.UtcNow;
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var currentPackage = await _userPackageRepository.GetCurrentByUserIdAsync(
            request.UserId, nowUtc, cancellationToken);
        var transactionType = DetermineTransactionType(currentPackage, request.PackageId);

        var snapshot = JsonSerializer.Serialize(new
        {
            packageId = package.Id,
            title = package.Title,
            price = package.Price,
            salePrice = package.SalePrice,
            type = package.Type,
            distanceLimitKm = package.DistanceLimitKm,
            durationInDays = package.DurationInDays
        });

        var transaction = new Transaction
        {
            UserId = request.UserId,
            PackageId = request.PackageId,
            Amount = amount,
            Type = transactionType,
            Status = "pending",
            ItemSnapshot = snapshot,
            OrderCode = orderCode.ToString(),
            PaymentMethod = "payos",
            CreatedAt = nowUtc
        };

        await _transactionRepository.CreateAsync(transaction, cancellationToken);

        var paymentRequest = new CreatePaymentRequest
        {
            OrderCode = orderCode,
            TotalAmount = amount,
            Description = package.Title.Length > 25
                ? package.Title[..25]
                : package.Title,
            ReturnUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl,
            Items =
            [
                new PaymentItemRequest
                {
                    Name = package.Title,
                    Quantity = 1,
                    Price = amount
                }
            ]
        };

        var paymentResult = await _payOSPaymentService.CreatePaymentLinkAsync(
            paymentRequest, cancellationToken);

        return new PurchasePackageResponse
        {
            TransactionId = transaction.Id,
            OrderCode = orderCode,
            CheckoutUrl = paymentResult.CheckoutUrl,
            QrCode = paymentResult.QrCode,
            Status = "pending"
        };
    }

    public async Task<ConfirmPaymentResponse> ConfirmPaymentAsync(
        long orderCode, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByOrderCodeAsync(
            orderCode.ToString(), cancellationToken);
        if (transaction == null)
            throw new ArgumentException("Không tìm thấy giao dịch.");

        if (transaction.Status == "completed")
            return new ConfirmPaymentResponse
            {
                TransactionId = transaction.Id,
                TransactionStatus = "completed"
            };

        var paymentInfo = await _payOSPaymentService.GetPaymentLinkAsync(
            orderCode.ToString(), cancellationToken);
        if (paymentInfo == null)
            throw new InvalidOperationException("Không thể xác minh trạng thái thanh toán.");

        if (paymentInfo.Status?.Equals("PAID", StringComparison.OrdinalIgnoreCase) == true)
        {
            transaction.Status = "completed";
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            var activationResult = await ActivatePackageAsync(transaction, cancellationToken);

            return new ConfirmPaymentResponse
            {
                TransactionId = transaction.Id,
                TransactionStatus = "completed",
                UserPackageId = activationResult.Id,
                ExpiresAt = activationResult.ExpiresAt
            };
        }

        if (paymentInfo.Status?.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) == true
            || paymentInfo.Status?.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase) == true)
        {
            transaction.Status = "failed";
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            return new ConfirmPaymentResponse
            {
                TransactionId = transaction.Id,
                TransactionStatus = "failed"
            };
        }

        return new ConfirmPaymentResponse
        {
            TransactionId = transaction.Id,
            TransactionStatus = "pending"
        };
    }

    private async Task<UserPackageModel> ActivatePackageAsync(
        Transaction transaction, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var package = transaction.Package;

        var currentPackage = await _userPackageRepository.GetCurrentByUserIdAsync(
            transaction.UserId, nowUtc, cancellationToken);

        int bonusDays = 0;
        if (currentPackage != null && currentPackage.ExpiresAt.HasValue)
        {
            var remaining = (currentPackage.ExpiresAt.Value - nowUtc).TotalDays;
            bonusDays = remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
        }

        if (currentPackage != null)
            await _userPackageRepository.DeactivateCurrentAsync(
                transaction.UserId, nowUtc, cancellationToken);

        var newUserPackage = new UserPackageModel
        {
            UserId = transaction.UserId,
            PackageId = transaction.PackageId,
            DistanceLimitKm = package.DistanceLimitKm,
            UsedKm = 0,
            IsActive = true,
            ActivatedAt = nowUtc,
            ExpiresAt = nowUtc.AddDays(package.DurationInDays + bonusDays)
        };

        return await _userPackageRepository.CreateAsync(newUserPackage, cancellationToken);
    }

    private static string DetermineTransactionType(UserPackageModel? currentPackage, Guid newPackageId)
    {
        if (currentPackage == null)
            return "purchase";

        return currentPackage.PackageId == newPackageId ? "renewal" : "upgrade";
    }
}
