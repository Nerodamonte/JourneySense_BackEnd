using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("packages")]
public partial class Package
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("price", TypeName = "numeric(12,2)")]
    public decimal Price { get; set; }

    [Column("sale_price", TypeName = "numeric(12,2)")]
    public decimal? SalePrice { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = null!; // basic | pro

    [Column("distance_limit_km")]
    public int DistanceLimitKm { get; set; }

    [Column("duration_in_days")]
    public int DurationInDays { get; set; }

    [Column("benefit", TypeName = "jsonb")]
    public string? Benefit { get; set; }

    [Column("is_popular")]
    public bool? IsPopular { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Package")]
    public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();

    [InverseProperty("Package")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
