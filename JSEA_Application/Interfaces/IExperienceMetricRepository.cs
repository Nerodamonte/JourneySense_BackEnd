namespace JSEA_Application.Interfaces;

/// <summary>Cập nhật tổng hợp experience_metrics theo visits và ratings.</summary>
public interface IExperienceMetricRepository
{
    /// <summary>Tăng total_visits (và last_visited_at) — mỗi lần có bản ghi visit mới cho experience.</summary>
    Task IncrementVisitCountAsync(Guid experienceId, CancellationToken cancellationToken = default);

    /// <summary>Ghi nhận một lượt đánh giá mới (total_ratings++, avg_rating rolling).</summary>
    Task AddRatingAsync(Guid experienceId, int stars1To5, CancellationToken cancellationToken = default);
}
