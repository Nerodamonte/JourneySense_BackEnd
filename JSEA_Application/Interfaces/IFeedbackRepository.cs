using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback> SaveAsync(Feedback feedback, CancellationToken cancellationToken = default);
}
