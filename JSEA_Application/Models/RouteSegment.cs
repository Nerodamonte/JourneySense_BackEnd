using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Models;

[Table("route_segments")]
public partial class RouteSegment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid? JourneyId { get; set; }

    [Column("segment_path", TypeName = "geography(LineString,4326)")]
    public LineString? SegmentPath { get; set; }

    [Column("segment_order")]
    public int? SegmentOrder { get; set; }

    [Column("distance_meters")]
    public int? DistanceMeters { get; set; }

    [Column("estimated_duration_minutes")]
    public int? EstimatedDurationMinutes { get; set; }

    [Column("is_scenic")]
    public bool? IsScenic { get; set; }

    [Column("is_busy")]
    public bool? IsBusy { get; set; }

    [Column("is_cultural_area")]
    public bool? IsCulturalArea { get; set; }

    [ForeignKey("JourneyId")]
    [InverseProperty("RouteSegments")]
    public virtual Journey? Journey { get; set; }

    [InverseProperty("Segment")]
    public virtual ICollection<JourneySuggestion> JourneySuggestions { get; set; } = new List<JourneySuggestion>();
}
