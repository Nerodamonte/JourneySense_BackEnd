using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("ratings")]
[Index("VisitId", Name = "ratings_visit_id_key", IsUnique = true)]
public partial class Rating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("visit_id")]
    public Guid? VisitId { get; set; }

    [Column("traveler_id")]
    public Guid? TravelerId { get; set; }

    [Column("experience_id")]
    public Guid? ExperienceId { get; set; }

    /// <summary>
    /// 1-5 stars
    /// </summary>
    [Column("rating")]
    public int? Rating1 { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("Ratings")]
    public virtual MicroExperience? Experience { get; set; }

    [ForeignKey("TravelerId")]
    [InverseProperty("Ratings")]
    public virtual User? Traveler { get; set; }

    [InverseProperty("Id1")]
    public virtual Visit? Visit { get; set; }
}
