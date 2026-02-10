using System;
using System.Collections.Generic;
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
    public Guid? UserId { get; set; }

    [Column("package_id")]
    public Guid? PackageId { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey("PackageId")]
    [InverseProperty("UserPackages")]
    public virtual Package? Package { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserPackages")]
    public virtual User? User { get; set; }
}
