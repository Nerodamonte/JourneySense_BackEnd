using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

/// <summary>
/// Metadata chi tiết cho từng loại user
/// </summary>
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

    [Column("preferred_travel_styles")]
    public List<string>? PreferredTravelStyles { get; set; }

    [Column("interests")]
    public List<string>? Interests { get; set; }

    [Column("accessibility_needs")]
    public string? AccessibilityNeeds { get; set; }

    [Column("department")]
    [StringLength(100)]
    public string? Department { get; set; }

    [Column("permissions", TypeName = "jsonb")]
    public string? Permissions { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Profile")]
    public virtual User? User { get; set; }
}
