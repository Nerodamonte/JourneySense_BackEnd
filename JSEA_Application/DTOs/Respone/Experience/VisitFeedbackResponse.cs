namespace JSEA_Application.DTOs.Respone.Experience;

/// <summary>
/// Kết quả sau khi user đánh dấu đã ghé thăm + gửi rating/feedback cho một Experience.
/// </summary>
public class VisitFeedbackResponse
{
    /// <summary>Id bản ghi Visit vừa tạo.</summary>
    public Guid VisitId { get; set; }

    /// <summary>Id bản ghi Rating (nếu user có gửi rating).</summary>
    public Guid? RatingId { get; set; }

    /// <summary>Id bản ghi Feedback (nếu user có gửi feedback/text hoặc ảnh).</summary>
    public Guid? FeedbackId { get; set; }

    /// <summary>Số điểm thưởng (nếu có chính sách reward).</summary>
    public int PointsEarned { get; set; }
}
