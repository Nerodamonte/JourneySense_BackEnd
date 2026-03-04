using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("experience_tags")]
public partial class ExperienceTag
{
    [Key]
    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Key]
    [Column("factor_id")]
    public Guid FactorId { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperienceTags")]
    public virtual Experience Experience { get; set; } = null!;

    [ForeignKey("FactorId")]
    [InverseProperty("ExperienceTags")]
    public virtual Factor Factor { get; set; } = null!;
}
