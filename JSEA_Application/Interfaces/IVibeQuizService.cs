using JSEA_Application.DTOs.Request.Quiz;
using JSEA_Application.DTOs.Respone.Quiz;

namespace JSEA_Application.Interfaces;

public interface IVibeQuizService
{
    VibeQuizResponse GetQuiz();

    Task<SubmitVibeQuizResponse> SubmitAsync(Guid userId, SubmitVibeQuizRequest request, CancellationToken cancellationToken = default);
}
