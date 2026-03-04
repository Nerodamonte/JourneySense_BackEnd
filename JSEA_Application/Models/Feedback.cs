using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("feedbacks")]
public partial class Feedback
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("visit_id")]
    public Guid VisitId { get; set; }

    [Column("feedback_text")]
    public string FeedbackText { get; set; } = null!;

    [Column("is_flagged")]
    public bool? IsFlagged { get; set; }

    [Column("flagged_reason")]
    public string? FlaggedReason { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("VisitId")]
    [InverseProperty("Feedback")]
    public virtual Visit Visit { get; set; } = null!;
}
