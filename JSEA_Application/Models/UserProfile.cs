using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("user_profiles")]
[Index("UserId", Name = "user_profiles_user_id_key", IsUnique = true)]
public partial class UserProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string? FullName { get; set; }

    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("accessibility_needs")]
    public string? AccessibilityNeeds { get; set; }

    [Column("department")]
    [StringLength(100)]
    public string? Department { get; set; }

    [Column("permissions", TypeName = "jsonb")]
    public string? Permissions { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserProfile")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("UserProfile")]
    public virtual ICollection<UserVibe> UserVibes { get; set; } = new List<UserVibe>();
}
