using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IAchievementRepository
{
    Task<Achievement?> GetActiveByCodeAsync(string code, CancellationToken cancellationToken = default);
}
