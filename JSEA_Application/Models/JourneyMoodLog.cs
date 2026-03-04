using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("journey_mood_logs")]
public partial class JourneyMoodLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("factor_id")]
    public Guid FactorId { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }

    [Column("trigger")]
    [StringLength(50)]
    public string Trigger { get; set; } = null!; // manual | auto | ai

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneyMoodLogs")]
    public virtual Journey Journey { get; set; } = null!;

    [ForeignKey("FactorId")]
    [InverseProperty("JourneyMoodLogs")]
    public virtual Factor Factor { get; set; } = null!;
}
