using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("journey_suggestions")]
public partial class JourneySuggestion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("segment_id")]
    public Guid SegmentId { get; set; }

    [Column("detour_distance_meters")]
    public int? DetourDistanceMeters { get; set; }

    [Column("detour_time_minutes")]
    public int? DetourTimeMinutes { get; set; }

    [Column("estimated_stop_minutes")]
    public int? EstimatedStopMinutes { get; set; }

    [Column("cosine_score", TypeName = "numeric(5,4)")]
    public decimal? CosineScore { get; set; }

    [Column("distance_score", TypeName = "numeric(5,4)")]
    public decimal? DistanceScore { get; set; }

    [Column("final_similarity", TypeName = "numeric(5,4)")]
    public decimal? FinalSimilarity { get; set; }

    [Column("suggested_at")]
    public DateTime? SuggestedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("JourneySuggestions")]
    public virtual Experience Experience { get; set; } = null!;

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneySuggestions")]
    public virtual Journey Journey { get; set; } = null!;

    [ForeignKey("SegmentId")]
    [InverseProperty("JourneySuggestions")]
    public virtual RouteSegment Segment { get; set; } = null!;

    [InverseProperty("Suggestion")]
    public virtual ICollection<SuggestionInteraction> SuggestionInteractions { get; set; } = new List<SuggestionInteraction>();

    [InverseProperty("Suggestion")]
    public virtual ICollection<JourneyWaypoint> JourneyWaypoints { get; set; } = new List<JourneyWaypoint>();
}
