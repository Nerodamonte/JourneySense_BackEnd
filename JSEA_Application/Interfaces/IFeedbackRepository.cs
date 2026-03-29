using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback> SaveAsync(Feedback feedback, CancellationToken cancellationToken = default);

    Task<Feedback?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);

    Task<Feedback?> GetByIdWithVisitAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    /// <summary>Staff portal: danh sách feedback, kèm Visit → Experience, Traveler.</summary>
    Task<(List<Feedback> Items, int TotalCount)> ListForStaffAsync(
        string? moderationStatus,
        Guid? experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy top N feedbacks mới nhất của một experience (đã duyệt, không bị flag). Dùng cho RAG prompt.</summary>
    Task<List<string>> GetTopByExperienceIdAsync(Guid experienceId, int topN, CancellationToken cancellationToken = default);

    /// <summary>
    /// Feedback công khai theo experience (đã duyệt). Có thể loại bỏ feedback của chính viewer.
    /// </summary>
    Task<(List<Feedback> Items, int TotalCount)> ListPublicApprovedForExperienceAsync(
        Guid experienceId,
        Guid? excludeTravelerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> TryModerateAsync(
        Guid feedbackId,
        string moderationStatus,
        bool isFlagged,
        string? flaggedReason,
        CancellationToken cancellationToken = default);
}
