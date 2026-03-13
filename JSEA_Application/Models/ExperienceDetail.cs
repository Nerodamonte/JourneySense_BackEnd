using System;
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

    [Column("rich_description")]
    public string? RichDescription { get; set; }

    [Column("opening_hours", TypeName = "jsonb")]
    public string? OpeningHours { get; set; }

    [Column("price_range")]
    [StringLength(20)]
    public string? PriceRange { get; set; }

    [Column("crowd_level")]
    [StringLength(20)]
    public string CrowdLevel { get; set; } = "normal"; // quiet|normal|busy

    [Column("safety_notes")]
    public string? SafetyNotes { get; set; }

    [Column("accessibility_info")]
    public string? AccessibilityInfo { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperienceDetail")]
    public virtual Experience Experience { get; set; } = null!;
}
