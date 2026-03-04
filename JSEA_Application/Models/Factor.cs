using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("factors")]
[Index("Name", "Type", Name = "factors_name_type_unique", IsUnique = true)]
public partial class Factor
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("type")]
    [StringLength(10)]
    public string Type { get; set; } = null!; // vibe | mood

    [Column("type_weight", TypeName = "numeric(3,2)")]
    public decimal TypeWeight { get; set; }

    [Column("name_weight")]
    public int NameWeight { get; set; }

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [InverseProperty("Factor")]
    public virtual ICollection<ExperienceTag> ExperienceTags { get; set; } = new List<ExperienceTag>();

    [InverseProperty("Factor")]
    public virtual ICollection<JourneyMoodLog> JourneyMoodLogs { get; set; } = new List<JourneyMoodLog>();

    [InverseProperty("Factor")]
    public virtual ICollection<Journey> Journeys { get; set; } = new List<Journey>();

    [InverseProperty("Factor")]
    public virtual ICollection<UserVibe> UserVibes { get; set; } = new List<UserVibe>();
}
