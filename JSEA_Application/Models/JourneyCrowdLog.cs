using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("journey_crowd_logs")]
public partial class JourneyCrowdLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("crowd_level")]
    [StringLength(20)]
    public string CrowdLevel { get; set; } = null!; // quiet | normal | busy

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }

    [Column("trigger")]
    [StringLength(50)]
    public string Trigger { get; set; } = null!; // manual | auto | ai

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneyCrowdLogs")]
    public virtual Journey Journey { get; set; } = null!;
}
