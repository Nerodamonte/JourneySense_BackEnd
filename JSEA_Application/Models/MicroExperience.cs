using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("micro_experiences")]
[Index("Slug", Name = "micro_experiences_slug_key", IsUnique = true)]
public partial class MicroExperience
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("uploaded_by_user_id")]
    public Guid? UploadedByUserId { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    [Column("slug")]
    [StringLength(255)]
    public string? Slug { get; set; }

    [Column("location", TypeName = "geography(Point,4326)")]
    public Point? Location { get; set; }

    [Column("address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("MicroExperiences")]
    public virtual Category? Category { get; set; }

    [InverseProperty("Experience")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [InverseProperty("Experience")]
    public virtual ICollection<ExperiencePhoto> ExperiencePhotos { get; set; } = new List<ExperiencePhoto>();

    [InverseProperty("Experience")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [ForeignKey("Id")]
    [InverseProperty("MicroExperience")]
    public virtual ExperienceMetric Id1 { get; set; } = null!;

    [ForeignKey("Id")]
    [InverseProperty("MicroExperience")]
    public virtual ExperienceDetail IdNavigation { get; set; } = null!;

    [InverseProperty("Experience")]
    public virtual ICollection<JourneySuggestion> JourneySuggestions { get; set; } = new List<JourneySuggestion>();

    [InverseProperty("Experience")]
    public virtual ICollection<JourneyWaypoint> JourneyWaypoints { get; set; } = new List<JourneyWaypoint>();

    [InverseProperty("RelatedExperience")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Experience")]
    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    [ForeignKey("UploadedByUserId")]
    [InverseProperty("MicroExperiences")]
    public virtual User? UploadedByUser { get; set; }

    [InverseProperty("Experience")]
    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

    [InverseProperty("Experience")]
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    [Column("status")]
    public ExperienceStatus Status { get; set; }

    [Column("suitable_moods")]
    public MoodType[] SuitableMoods { get; set; }

    [Column("preferred_times")]
    public TimeOfDay[] PreferredTimes { get; set; }

    [Column("weather_suitability")]
    public WeatherType[] WeatherSuitability { get; set; }

    [Column("seasonality")]
    public SeasonType[] Seasonality { get; set; }
}
