using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Payment;

public class CreatePaymentRequest
{
    public long? OrderCode { get; set; }

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public long TotalAmount { get; set; }

    public string? Description { get; set; }

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public string? BuyerName { get; set; }

    public string? BuyerEmail { get; set; }

    public string? BuyerPhone { get; set; }

    [Required]
    public List<PaymentItemRequest> Items { get; set; } = [];
}

public class PaymentItemRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, long.MaxValue)]
    public long Price { get; set; }

    public string? Unit { get; set; }
}
