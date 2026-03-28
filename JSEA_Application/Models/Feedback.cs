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

    /// <summary>pending | approved | rejected — chỉ approved mới dùng trong RAG/mobile công khai.</summary>
    [Column("moderation_status")]
    [StringLength(20)]
    public string ModerationStatus { get; set; } = "approved";

    [Column("flagged_reason")]
    public string? FlaggedReason { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("VisitId")]
    [InverseProperty("Feedback")]
    public virtual Visit Visit { get; set; } = null!;
}
