using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("journeys")]
public partial class Journey
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("traveler_id")]
    public Guid TravelerId { get; set; }

    [Column("origin_location", TypeName = "geography(Point,4326)")]
    public Point OriginLocation { get; set; } = null!;

    [Column("origin_address")]
    [StringLength(500)]
    public string? OriginAddress { get; set; }

    [Column("destination_location", TypeName = "geography(Point,4326)")]
    public Point DestinationLocation { get; set; } = null!;

    [Column("destination_address")]
    [StringLength(500)]
    public string? DestinationAddress { get; set; }

    [Column("route_path", TypeName = "geography(LineString,4326)")]
    public LineString? RoutePath { get; set; }

    [Column("actual_route_path", TypeName = "geography(LineString,4326)")]
    public LineString? ActualRoutePath { get; set; }

    [Column("total_distance_meters")]
    public int? TotalDistanceMeters { get; set; }

    [Column("actual_distance_meters")]
    public int? ActualDistanceMeters { get; set; }

    [Column("estimated_duration_minutes")]
    public int? EstimatedDurationMinutes { get; set; }

    [Column("current_mood")]
    [StringLength(20)]
    public string? CurrentMood { get; set; }

    [Column("preferred_crowd_level")]
    [StringLength(20)]
    public string PreferredCrowdLevel { get; set; } = "all"; // all|quiet|normal|busy

    [Column("vehicle_type")]
    [StringLength(20)]
    public string VehicleType { get; set; } = null!; // walking|bicycle|motorbike|car

    [Column("max_detour_distance_meters")]
    public int MaxDetourDistanceMeters { get; set; }

    [Column("time_budget_minutes")]
    public int? TimeBudgetMinutes { get; set; }

    [Column("max_stops")]
    public int? MaxStops { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "planning"; // planning|in_progress|completed|cancelled

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("journey_feedback")]
    public string? JourneyFeedback { get; set; }

    [InverseProperty("Journey")]
    public virtual ICollection<JourneyCrowdLog> JourneyCrowdLogs { get; set; } = new List<JourneyCrowdLog>();

    [InverseProperty("Journey")]
    public virtual ICollection<JourneyMoodLog> JourneyMoodLogs { get; set; } = new List<JourneyMoodLog>();

    [InverseProperty("Journey")]
    public virtual ICollection<JourneySuggestion> JourneySuggestions { get; set; } = new List<JourneySuggestion>();

    [InverseProperty("Journey")]
    public virtual ICollection<JourneyWaypoint> JourneyWaypoints { get; set; } = new List<JourneyWaypoint>();

    [InverseProperty("Journey")]
    public virtual ICollection<RouteSegment> RouteSegments { get; set; } = new List<RouteSegment>();

    [ForeignKey("TravelerId")]
    [InverseProperty("Journeys")]
    public virtual User Traveler { get; set; } = null!;

    [InverseProperty("Journey")]
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    [InverseProperty("Journey")]
    public virtual ICollection<SharedJourney> SharedJourneys { get; set; } = new List<SharedJourney>();
}
