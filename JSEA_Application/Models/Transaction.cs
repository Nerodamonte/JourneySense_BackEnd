using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("transactions")]
public partial class Transaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("package_id")]
    public Guid PackageId { get; set; }

    [Column("amount")]
    public long Amount { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = null!; // purchase | renewal | upgrade

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = null!; // pending | completed | failed | refunded

    [Column("item_snapshot", TypeName = "jsonb")]
    public string ItemSnapshot { get; set; } = null!;

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("PackageId")]
    [InverseProperty("Transactions")]
    public virtual Package Package { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Transactions")]
    public virtual User User { get; set; } = null!;
}
