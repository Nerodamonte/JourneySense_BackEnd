namespace JSEA_Application.DTOs.Respone.Experience;

public class VisitFeedbackResponse
{
    public Guid VisitId { get; set; }
    public Guid? RatingId { get; set; }
    public Guid? FeedbackId { get; set; }
    public int PointsEarned { get; set; }
}
