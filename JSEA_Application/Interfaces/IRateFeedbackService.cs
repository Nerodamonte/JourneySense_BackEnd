using JSEA_Application.DTOs.Request.Experience;
using JSEA_Application.DTOs.Respone.Experience;

namespace JSEA_Application.Interfaces;

public interface IRateFeedbackService
{
    /// <summary>
    /// Tạo visit + rating (nếu có) + feedback (nếu có), cộng điểm thưởng. Trả về null nếu đã từng visit (travelerId, experienceId, journeyId).
    /// </summary>
    Task<VisitFeedbackResponse?> CreateVisitWithFeedbackAsync(VisitFeedbackRequest request, Guid travelerId, CancellationToken cancellationToken = default);
}
