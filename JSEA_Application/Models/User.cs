using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

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

    [Column("role")]
    [StringLength(50)]
    public string Role { get; set; } = null!; // traveler | staff | admin

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = null!; // pending_verification | active | suspended | deleted

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

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<ExperiencePhoto> ExperiencePhotos { get; set; } = new List<ExperiencePhoto>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [InverseProperty("Traveler")]
    public virtual ICollection<Journey> Journeys { get; set; } = new List<Journey>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    [InverseProperty("User")]
    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

    [InverseProperty("User")]
    public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();

    [InverseProperty("User")]
    public virtual UserProfile? UserProfile { get; set; }

    [InverseProperty("Traveler")]
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    [InverseProperty("User")]
    public virtual ICollection<SharedJourney> SharedJourneys { get; set; } = new List<SharedJourney>();

    [InverseProperty("User")]
    public virtual ICollection<RewardTransaction> RewardTransactions { get; set; } = new List<RewardTransaction>();
}
