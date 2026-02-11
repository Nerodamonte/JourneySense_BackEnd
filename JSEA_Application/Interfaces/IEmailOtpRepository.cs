using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface IEmailOtpRepository
    {
        Task AddAsync(EmailOtp otp);
        Task UpdateAsync(EmailOtp otp);
        Task<EmailOtp?> GetValidOtpAsync(string email, string otp);
        Task MarkUsedAsync(EmailOtp otp);
        Task MarkAllUsedByEmailAsync(string email);

        Task<bool> HasVerifiedOtpAsync(string email);
        Task<EmailOtp?> GetValidOtpByCodeAsync(string otp);
    }
}
