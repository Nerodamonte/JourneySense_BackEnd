using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("reward_transactions")]
public partial class RewardTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("achievement_id")]
    public Guid? AchievementId { get; set; }

    [Column("type")]
    [StringLength(20)]
    public string Type { get; set; } = null!;

    [Column("points")]
    public int Points { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("ref_id")]
    public Guid? RefId { get; set; }

    [Column("ref_type")]
    [StringLength(20)]
    public string? RefType { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AchievementId")]
    [InverseProperty("RewardTransactions")]
    public virtual Achievement? Achievement { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RewardTransactions")]
    public virtual User User { get; set; } = null!;
}
