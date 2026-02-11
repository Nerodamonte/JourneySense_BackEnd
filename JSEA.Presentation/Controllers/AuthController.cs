using JSEA_Application.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JSEA_Application.DTOs.Request.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace JSEA_Presentation.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailOtpService _emailOtpService;


        public AuthController(IAuthService authService, IEmailOtpService emailOtpService)
        {
            _authService = authService;
            _emailOtpService = emailOtpService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(
                    request.Email,
                    request.Password
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> SendRegisterOtp(
          [FromBody] RegisterEmailRequest request
      )
        {
            await _emailOtpService.SendOtpAsync(request.Email);

            // Không tiết lộ email tồn tại hay không
            return Ok(new
            {
                message = " Mã OTP đã được gửi!"
            });
        }

        // ================= REGISTER - STEP 2 =================
        // Verify OTP
        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> VerifyOtp(
          [FromBody] VerifyOtpRequest request
)
        {
            var token = await _emailOtpService
                .VerifyOtpAndGenerateRegisterTokenAsync(request.Otp);

            return Ok(new
            {
                registerToken = token
            });
        }

        [Authorize(AuthenticationSchemes = "Register")]
        [HttpPost("register/set-password")]
        public async Task<IActionResult> SetPassword(
         [FromBody] SetPasswordRequest request
)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)!;

            await _authService.RegisterSetPasswordAsync(
                email,
                request.Password,
                request.ConfirmPassword
            );

            return Ok(new
            {
                message = "Thiết lập mật khẩu thành công. Vui lòng đăng nhập."
            });
        }

    }
}
