using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("events")]
public partial class Event
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("event_type")]
    [StringLength(100)]
    public string? EventType { get; set; }

    [Column("start_datetime")]
    public DateTime StartDatetime { get; set; }

    [Column("end_datetime")]
    public DateTime EndDatetime { get; set; }

    [Column("recurrence_pattern")]
    [StringLength(50)]
    public string RecurrencePattern { get; set; } = null!; // once|daily|weekly|monthly|yearly|custom

    [Column("recurrence_rule")]
    public string? RecurrenceRule { get; set; }

    [Column("score_boost_factor", TypeName = "numeric(3,2)")]
    public decimal? ScoreBoostFactor { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Events")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("Events")]
    public virtual Experience Experience { get; set; } = null!;

    [InverseProperty("Event")]
    public virtual ICollection<EventOccurrence> EventOccurrences { get; set; } = new List<EventOccurrence>();
}
