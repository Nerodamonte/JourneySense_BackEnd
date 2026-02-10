using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("feedbacks")]
[Index("VisitId", Name = "feedbacks_visit_id_key", IsUnique = true)]
public partial class Feedback
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("visit_id")]
    public Guid? VisitId { get; set; }

    [Column("traveler_id")]
    public Guid? TravelerId { get; set; }

    [Column("experience_id")]
    public Guid? ExperienceId { get; set; }

    [Column("feedback_text")]
    public string? FeedbackText { get; set; }

    [Column("is_flagged")]
    public bool? IsFlagged { get; set; }

    [Column("flagged_reason")]
    public string? FlaggedReason { get; set; }

    [Column("is_approved")]
    public bool? IsApproved { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("Feedbacks")]
    public virtual MicroExperience? Experience { get; set; }

    [ForeignKey("TravelerId")]
    [InverseProperty("Feedbacks")]
    public virtual User? Traveler { get; set; }

    
    [ForeignKey("VisitId")]
    [InverseProperty("Feedback")]
    public virtual Visit? Visit { get; set; }
}