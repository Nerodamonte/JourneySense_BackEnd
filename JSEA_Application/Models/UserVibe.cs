using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("user_vibes")]
public partial class UserVibe
{
    [Key]
    [Column("user_profile_id")]
    public Guid UserProfileId { get; set; }

    [Key]
    [Column("factor_id")]
    public Guid FactorId { get; set; }

    [Column("selected_at")]
    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserProfileId")]
    [InverseProperty("UserVibes")]
    public virtual UserProfile UserProfile { get; set; } = null!;

    [ForeignKey("FactorId")]
    [InverseProperty("UserVibes")]
    public virtual Factor Factor { get; set; } = null!;
}
