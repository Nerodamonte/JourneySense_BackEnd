using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("email_otps")]
public partial class EmailOtp
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [Column("otp_code")]
    [StringLength(6)]
    public string OtpCode { get; set; } = null!;

    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    [Column("is_used")]
    public bool? IsUsed { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}