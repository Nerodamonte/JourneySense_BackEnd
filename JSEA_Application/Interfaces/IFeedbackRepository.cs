using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback> SaveAsync(Feedback feedback, CancellationToken cancellationToken = default);

    /// <summary>Lấy top N feedbacks mới nhất của một experience (không bị flag). Dùng cho RAG prompt.</summary>
    Task<List<string>> GetTopByExperienceIdAsync(Guid experienceId, int topN, CancellationToken cancellationToken = default);
}
