using JSEA_Application.DTOs.Request.Payment;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPayOSPaymentService _paymentService;

    public PaymentController(IPayOSPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Tạo link thanh toán PayOS. Trả về CheckoutUrl để chuyển hướng người dùng thanh toán.
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(JSEA_Application.DTOs.Respone.Payment.CreatePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu đơn hàng không được để trống." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _paymentService.CreatePaymentLinkAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Tạo link thanh toán thất bại.", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin/trạng thái link thanh toán theo PaymentLinkId (trả về từ API create).
    /// </summary>
    [HttpGet("link/{paymentLinkId}")]
    [ProducesResponseType(typeof(JSEA_Application.DTOs.Respone.Payment.PaymentLinkInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentLink(string paymentLinkId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentLinkId))
            return BadRequest(new { message = "PaymentLinkId không hợp lệ." });

        var result = await _paymentService.GetPaymentLinkAsync(paymentLinkId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy link thanh toán hoặc đã hết hạn." });

        return Ok(result);
    }

    /// <summary>
    /// Hủy link thanh toán (chỉ khi chưa thanh toán).
    /// </summary>
    [HttpPost("link/{paymentLinkId}/cancel")]
    [ProducesResponseType(typeof(JSEA_Application.DTOs.Respone.Payment.PaymentLinkInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPaymentLink(
        string paymentLinkId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentLinkId))
            return BadRequest(new { message = "PaymentLinkId không hợp lệ." });

        var result = await _paymentService.CancelPaymentLinkAsync(paymentLinkId, reason, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không thể hủy link thanh toán hoặc đã thanh toán." });

        return Ok(result);
    }
}
