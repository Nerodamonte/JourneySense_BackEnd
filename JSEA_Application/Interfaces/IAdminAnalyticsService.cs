using JSEA_Application.DTOs.Portal;

namespace JSEA_Application.Interfaces;

public interface IAdminAnalyticsService
{
    Task<AdminAnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
