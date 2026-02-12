using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("email_otps")]
public partial class EmailOtp
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("otp_code")]
    [StringLength(10)]
    public string OtpCode { get; set; } = null!;

    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
