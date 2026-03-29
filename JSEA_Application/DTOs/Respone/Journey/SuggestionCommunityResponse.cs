namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>Metrics từ experience_metrics (thống kê địa điểm).</summary>
public class ExperienceSocialMetricsDto
{
    public int? TotalVisits { get; set; }
    public int? TotalRatings { get; set; }
    public decimal? AvgRating { get; set; }
}

/// <summary>Feedback đã duyệt hiển thị công khai cho người xem chi tiết gợi ý.</summary>
public class PublicExperienceFeedbackItemDto
{
    public Guid FeedbackId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }

    /// <summary>Số sao (1–5) nếu user đã đánh giá cùng visit; null nếu chỉ có bình luận.</summary>
    public int? Stars { get; set; }

    /// <summary>Tên hiển thị từ hồ sơ; null nếu không có.</summary>
    public string? AuthorDisplayName { get; set; }
}

/// <summary>
/// Dữ liệu cộng đồng khi mở chi tiết một suggestion: metrics + feedback người khác.
/// </summary>
public class SuggestionCommunityResponse
{
    public Guid ExperienceId { get; set; }
    public string ExperienceName { get; set; } = null!;

    public ExperienceSocialMetricsDto Metrics { get; set; } = new();

    public IReadOnlyList<PublicExperienceFeedbackItemDto> Feedbacks { get; set; } = [];

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalFeedbacks { get; set; }
}
