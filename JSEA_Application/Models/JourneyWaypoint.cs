using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("journey_waypoints")]
public partial class JourneyWaypoint
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("suggestion_id")]
    public Guid? SuggestionId { get; set; }

    [Column("stop_order")]
    public int StopOrder { get; set; }

    [Column("actual_stop_minutes")]
    public int? ActualStopMinutes { get; set; }

    [Column("estimated_arrival_at")]
    public DateTime? EstimatedArrivalAt { get; set; }

    [Column("actual_arrival_at")]
    public DateTime? ActualArrivalAt { get; set; }

    [Column("actual_departure_at")]
    public DateTime? ActualDepartureAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("JourneyWaypoints")]
    public virtual Experience Experience { get; set; } = null!;

    [ForeignKey("JourneyId")]
    [InverseProperty("JourneyWaypoints")]
    public virtual Journey Journey { get; set; } = null!;

    [ForeignKey("SuggestionId")]
    [InverseProperty("JourneyWaypoints")]
    public virtual JourneySuggestion? Suggestion { get; set; }
}
