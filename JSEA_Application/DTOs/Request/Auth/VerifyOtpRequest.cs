using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.DTOs.Request.Auth
{
    public class VerifyOtpRequest
    {
        public string Otp { get; set; } = null!;
    }
}
