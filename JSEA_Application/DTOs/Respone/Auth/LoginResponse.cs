using JSEA_Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.DTOs.Respone.Auth
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
