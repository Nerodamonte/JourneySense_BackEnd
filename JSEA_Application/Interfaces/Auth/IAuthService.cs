using JSEA_Application.DTOs.Respone.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(string email, string password);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken); 
        Task LogoutAsync(string refreshToken); 
    }
}
