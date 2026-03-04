using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("experience_metrics")]
public partial class ExperienceMetric
{
    [Key]
    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("featured_score")]
    [Precision(3, 2)]
    public decimal? FeaturedScore { get; set; }

    [Column("total_visits")]
    public int? TotalVisits { get; set; }

    [Column("total_ratings")]
    public int? TotalRatings { get; set; }

    [Column("avg_rating")]
    [Precision(2, 1)]
    public decimal? AvgRating { get; set; }

    [Column("acceptance_rate")]
    [Precision(3, 2)]
    public decimal? AcceptanceRate { get; set; }

    [Column("last_visited_at")]
    public DateTime? LastVisitedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperienceMetric")]
    public virtual Experience Experience { get; set; } = null!;
}
