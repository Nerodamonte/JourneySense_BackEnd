using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("user_favorites")]
public partial class UserFavorite
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("UserFavorites")]
    public virtual MicroExperience Experience { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserFavorites")]
    public virtual User User { get; set; } = null!;
}
