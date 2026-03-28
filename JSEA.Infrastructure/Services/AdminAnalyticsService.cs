using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Services;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly AppDbContext _db;

    public AdminAnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminAnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var usersBase = _db.Users.AsNoTracking().Where(u => u.DeletedAt == null);
        var pendingFeedback = _db.Feedbacks.AsNoTracking().Count(f => f.ModerationStatus == "pending");

        return new AdminAnalyticsSummaryResponse
        {
            UsersTotal = await usersBase.CountAsync(cancellationToken),
            UsersActive = await usersBase.CountAsync(u => u.Status == "active", cancellationToken),
            UsersTraveler = await usersBase.CountAsync(u => u.Role == AppRoles.Traveler, cancellationToken),
            UsersStaff = await usersBase.CountAsync(u => u.Role == AppRoles.Staff, cancellationToken),
            UsersAdmin = await usersBase.CountAsync(u => u.Role == AppRoles.Admin, cancellationToken),
            ExperiencesActive = await _db.Experiences.AsNoTracking().CountAsync(e => e.Status == "active", cancellationToken),
            JourneysTotal = await _db.Journeys.AsNoTracking().CountAsync(cancellationToken),
            FeedbacksPendingModeration = pendingFeedback
        };
    }
}
