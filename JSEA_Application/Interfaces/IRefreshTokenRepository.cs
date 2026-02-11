using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> AddAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task UpdateAsync(RefreshToken refreshToken);
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);
    }
}
