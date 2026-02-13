using JSEA_Application.Interfaces.Auth;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Services.Auth
{
    public class EmailOtpService : IEmailOtpService
    {
        private readonly IEmailOtpRepository _otpRepository;
        private readonly IJwtService _jwtService;
        private readonly IEmailSender _emailSender;
        private readonly IUserRepository _userRepository;

        public EmailOtpService(IEmailOtpRepository otpRepository, IJwtService jwtService, IEmailSender emailSender, IUserRepository userRepository)
        {
            _otpRepository = otpRepository;
            _jwtService = jwtService;
            _emailSender = emailSender;
            _userRepository = userRepository;
        }

        public async Task SendOtpAsync(string email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                throw new Exception("Email đã được đăng ký");

            var activeOtp = await _otpRepository.GetLatestActiveOtpAsync(email);

            if (activeOtp != null)
            {
                var secondsSinceLastSend =
                    (DateTime.UtcNow - activeOtp.CreatedAt).TotalSeconds;

                if (secondsSinceLastSend < 60)
                    throw new Exception("Vui lòng đợi 60 giây trước khi gửi lại OTP.");
            }
            await _otpRepository.InvalidateAllActiveOtpAsync(email);

            var otp = new Random().Next(100000, 999999).ToString();

            var entity = new EmailOtp
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                IsVerified = false
            };

            await _otpRepository.AddAsync(entity);

            var body = $@"
        <h2>Journey Sense</h2>
        <p>Mã OTP của bạn là:</p>
        <h1>{otp}</h1>
        <p>OTP có hiệu lực trong 5 phút.</p>
    ";

            await _emailSender.SendAsync(
                email,
                "Mã xác thực OTP",
                body
            );
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var record = await _otpRepository.GetValidOtpAsync(email, otp);

            if (record == null)
                return false;

            record.IsVerified = true;
            await _otpRepository.UpdateAsync(record);
            return true;
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<string> VerifyOtpAndGenerateRegisterTokenAsync(string otp)
        {
            // 1️⃣ Tìm OTP hợp lệ
            var emailOtp = await _otpRepository.GetValidOtpByCodeAsync(otp);

            if (emailOtp == null)
                throw new Exception("OTP không hợp lệ hoặc đã hết hạn");

            // 2️⃣ Đánh dấu OTP đã dùng
            emailOtp.IsUsed = true;
            emailOtp.IsVerified = true;


            await _otpRepository.UpdateAsync(emailOtp);

            // 3️⃣ Generate register token
            return _jwtService.GenerateRegisterToken(emailOtp.Email);
        }

        public async Task ResendRegisterOtpAsync(string email)
        {

            var activeOtp = await _otpRepository.GetLatestActiveOtpAsync(email);

            if (activeOtp != null)
            {
                var secondsSinceLastSend =
                    (DateTime.UtcNow - activeOtp.CreatedAt).TotalSeconds;

                if (secondsSinceLastSend < 60)
                    throw new Exception("Vui lòng đợi 60 giây trước khi gửi lại OTP.");
            }


            await _otpRepository.InvalidateAllActiveOtpAsync(email);

            var otpCode = GenerateOtp();

            var otp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                IsVerified = false
            };

            await _otpRepository.AddAsync(otp);

            var body = $@"
        <h2>Journey Sense</h2>
        <p>Mã OTP mới của bạn là:</p>
        <h1>{otpCode}</h1>
        <p>OTP có hiệu lực trong 5 phút.</p>
    ";

            await _emailSender.SendAsync(
                email,
                "Mã xác thực OTP (Resend)",
                body
            );
        }
    }
}
