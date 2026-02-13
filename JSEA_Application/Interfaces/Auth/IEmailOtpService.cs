using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces.Auth
{
    public interface IEmailOtpService
    {
        Task SendOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);
        Task<string> VerifyOtpAndGenerateRegisterTokenAsync(string otp);

        Task ResendRegisterOtpAsync(string email);
    }
}
