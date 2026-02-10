using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

/// <summary>
/// Quản lý authentication và phân quyền
/// </summary>
[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("Phone", Name = "users_phone_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("password_hash")]
    [StringLength(255)]
    public string? PasswordHash { get; set; }

    [Column("reset_password_token")]
    public string? ResetPasswordToken { get; set; }

    [Column("reset_password_expiry")]
    public DateTime? ResetPasswordExpiry { get; set; }

    [Column("auth_provider")]
    [StringLength(50)]
    public string? AuthProvider { get; set; } 

    [Column("provider_key")]
    [StringLength(255)]
    public string? ProviderKey { get; set; } 

    
    [Column("access_failed_count")]
    public int AccessFailedCount { get; set; } = 0;


    [Column("email_verified")]
    public bool? EmailVerified { get; set; }

    [Column("phone_verified")]
    public bool? PhoneVerified { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("role")]
    public UserRole Role { get; set; }

    [Column("status")]
    public UserStatus Status { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [InverseProperty("FeaturedByUser")]
    public virtual ICollection<ExperienceDetail> ExperienceDetailFeaturedByUsers { get; set; } = new List<ExperienceDetail>();

    [InverseProperty("VerifiedByUser")]
    public virtual ICollection<ExperienceDetail> ExperienceDetailVerifiedByUsers { get; set; } = new List<ExperienceDetail>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<ExperiencePhoto> ExperiencePhotos { get; set; } = new List<ExperiencePhoto>();

    [InverseProperty("Traveler")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [ForeignKey("Id")]
    [InverseProperty("User")]
    public virtual UserProfile IdNavigation { get; set; } = null!;

    [InverseProperty("Traveler")]
    public virtual ICollection<Journey> Journeys { get; set; } = new List<Journey>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<MicroExperience> MicroExperiences { get; set; } = new List<MicroExperience>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Traveler")]
    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("UpdatedByUser")]
    public virtual ICollection<SystemConfig> SystemConfigs { get; set; } = new List<SystemConfig>();

    [InverseProperty("User")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    [InverseProperty("User")]
    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

    [InverseProperty("User")]
    public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();

    [InverseProperty("Traveler")]
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    
}
