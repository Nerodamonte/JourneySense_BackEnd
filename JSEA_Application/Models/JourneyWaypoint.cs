using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Models;

[Table("journey_waypoints")]
public partial class JourneyWaypoint
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid? JourneyId { get; set; }

    /// <summary>
    /// Có thể là 1 micro-experience hoặc điểm tùy chỉnh
    /// </summary>
    [Column("experience_id")]
    public Guid? ExperienceId { get; set; }

    [Column("location", TypeName = "geography(Point,4326)")]
    public Point? Location { get; set; }

    [Column("address")]
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Thứ tự điểm dừng 1, 2, 3...
    /// </summary>
    [Column("stop_order")]
    public int? StopOrder { get; set; }

    [Column("estimated_arrival_at")]
    public DateTime? EstimatedArrivalAt { get; set; }

    [Column("actual_arrival_at")]
    public DateTime? ActualArrivalAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("JourneyWaypoints")]
    public virtual MicroExperience? Experience { get; set; }

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneyWaypoints")]
    public virtual Journey? Journey { get; set; }
}
