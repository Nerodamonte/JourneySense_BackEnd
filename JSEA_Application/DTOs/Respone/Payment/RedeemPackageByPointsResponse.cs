namespace JSEA_Application.DTOs.Respone.Payment;

public class RedeemPackageByPointsResponse
{
    public Guid UserPackageId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RemainingRewardPoints { get; set; }
}
