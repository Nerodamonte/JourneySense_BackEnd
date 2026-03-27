using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Reward;

public class RewardPointsAdjustRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int Points { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = null!;

    public Guid? AchievementId { get; set; }

    public Guid? RefId { get; set; }

    [StringLength(50)]
    public string? RefType { get; set; }
}
