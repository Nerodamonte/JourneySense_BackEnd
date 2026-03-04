using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("user_packages")]
public partial class UserPackage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("package_id")]
    public Guid PackageId { get; set; }

    [Column("distance_limit_km")]
    public int DistanceLimitKm { get; set; }

    [Column("used_km", TypeName = "numeric(10,2)")]
    public decimal UsedKm { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey("PackageId")]
    [InverseProperty("UserPackages")]
    public virtual Package Package { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserPackages")]
    public virtual User User { get; set; } = null!;
}
