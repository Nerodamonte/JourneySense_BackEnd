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
    public Guid? JourneyId { get; set; }

    [Column("experience_id")]
    public Guid? ExperienceId { get; set; }

    [Column("segment_id")]
    public Guid? SegmentId { get; set; }

    [Column("detour_distance_meters")]
    public int? DetourDistanceMeters { get; set; }

    [Column("estimated_stop_minutes")]
    public int? EstimatedStopMinutes { get; set; }

    [Column("relevance_score")]
    [Precision(3, 2)]
    public decimal? RelevanceScore { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("suggested_at")]
    public DateTime? SuggestedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("JourneySuggestions")]
    public virtual MicroExperience? Experience { get; set; }

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneySuggestions")]
    public virtual Journey? Journey { get; set; }

    [ForeignKey("SegmentId")]
    [InverseProperty("JourneySuggestions")]
    public virtual RouteSegment? Segment { get; set; }

    [InverseProperty("Suggestion")]
    public virtual ICollection<SuggestionInteraction> SuggestionInteractions { get; set; } = new List<SuggestionInteraction>();
}
