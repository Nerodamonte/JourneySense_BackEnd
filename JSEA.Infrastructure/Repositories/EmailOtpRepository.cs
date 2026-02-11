using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace JSEA_Infrastructure.Repositories
{
    public class EmailOtpRepository : IEmailOtpRepository
    {
        private readonly AppDbContext _context;

        public EmailOtpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EmailOtp otp)
        {
            _context.EmailOtps.Add(otp);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailOtp otp)
        {
            otp.CreatedAt = DateTime.SpecifyKind(otp.CreatedAt, DateTimeKind.Utc);
            otp.ExpiredAt = DateTime.SpecifyKind(otp.ExpiredAt, DateTimeKind.Utc);

            _context.EmailOtps.Update(otp);
            await _context.SaveChangesAsync();
        }

        public async Task<EmailOtp?> GetValidOtpAsync(string email, string otp)
        {
            return await _context.EmailOtps
                .Where(x =>
                    x.Email == email &&
                    x.OtpCode == otp &&
                    !x.IsUsed &&
                    x.ExpiredAt > DateTime.UtcNow
                )
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasVerifiedOtpAsync(string email)
        {
            return await _context.EmailOtps.AnyAsync(x =>
                x.Email == email &&
                x.IsVerified == true &&
                x.IsUsed == false &&
                x.ExpiredAt > DateTime.UtcNow
            );
        }

        public async Task MarkAllUsedByEmailAsync(string email)
        {
            var otps = await _context.EmailOtps
                .Where(x => x.Email == email && x.IsVerified && !x.IsUsed)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkUsedAsync(EmailOtp otp)
        {
            otp.IsUsed = true;
            await _context.SaveChangesAsync();
        }

        public async Task<EmailOtp?> GetValidOtpByCodeAsync(string otp)
        {
            return await _context.EmailOtps
                .Where(x =>
                    x.OtpCode == otp &&
                    !x.IsUsed &&
                    x.ExpiredAt > DateTime.UtcNow
                )
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
