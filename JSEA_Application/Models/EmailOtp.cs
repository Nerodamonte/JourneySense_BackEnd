using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Models
{
    [Table("email_otps")]
    public class EmailOtp
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("otp_code")]
        public string OtpCode { get; set; } = null!;

        [Column("expired_at")]
        public DateTime ExpiredAt { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
