using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("visits")]
public partial class Visit
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("traveler_id")]
    public Guid? TravelerId { get; set; }

    [Column("experience_id")]
    public Guid? ExperienceId { get; set; }

    [Column("journey_id")]
    public Guid? JourneyId { get; set; }

    [Column("visited_at")]
    public DateTime? VisitedAt { get; set; }

    [Column("actual_duration_minutes")]
    public int? ActualDurationMinutes { get; set; }

    [Column("photo_urls")]
    public List<string>? PhotoUrls { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("Visits")]
    public virtual MicroExperience? Experience { get; set; }

    [ForeignKey("JourneyId")]
    [InverseProperty("Visits")]
    public virtual Journey? Journey { get; set; }

    [ForeignKey("TravelerId")]
    [InverseProperty("Visits")]
    public virtual User? Traveler { get; set; }

 
    [InverseProperty("Visit")]
    public virtual Rating? Rating { get; set; }

    [InverseProperty("Visit")]
    public virtual Feedback? Feedback { get; set; }
}