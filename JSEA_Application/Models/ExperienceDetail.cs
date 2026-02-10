using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("experience_details")]
public partial class ExperienceDetail
{
    [Key]
    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("opening_hours", TypeName = "jsonb")]
    public string? OpeningHours { get; set; }

    [Column("price_range")]
    [StringLength(20)]
    public string? PriceRange { get; set; }

    [Column("crowd_level")]
    [StringLength(20)]
    public string? CrowdLevel { get; set; }

    [Column("safety_notes")]
    public string? SafetyNotes { get; set; }

    [Column("accessibility_info")]
    public string? AccessibilityInfo { get; set; }

    [Column("moderation_notes")]
    public string? ModerationNotes { get; set; }

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("verified_by_user_id")]
    public Guid? VerifiedByUserId { get; set; }

    [Column("featured_at")]
    public DateTime? FeaturedAt { get; set; }

    [Column("featured_by_user_id")]
    public Guid? FeaturedByUserId { get; set; }

    [ForeignKey("FeaturedByUserId")]
    [InverseProperty("ExperienceDetailFeaturedByUsers")]
    public virtual User? FeaturedByUser { get; set; }

   
    [ForeignKey("ExperienceId")]
    [InverseProperty("Details")]
    public virtual MicroExperience? MicroExperience { get; set; }

    [ForeignKey("VerifiedByUserId")]
    [InverseProperty("ExperienceDetailVerifiedByUsers")]
    public virtual User? VerifiedByUser { get; set; }
}