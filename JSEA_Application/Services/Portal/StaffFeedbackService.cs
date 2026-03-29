using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System.Net;

namespace JSEA_Application.Services.Portal;

public class StaffFeedbackService : IStaffFeedbackService
{
    private readonly IFeedbackRepository _feedbacks;
    private readonly IUserRepository _users;
    private readonly IPortalAuditLogger _audit;
    private readonly IJourneyRepository _journeys;

    public StaffFeedbackService(
        IFeedbackRepository feedbacks,
        IUserRepository users,
        IPortalAuditLogger audit,
        IJourneyRepository journeys)
    {
        _feedbacks = feedbacks;
        _users = users;
        _audit = audit;
        _journeys = journeys;
    }

    public async Task<PortalPagedResult<StaffFeedbackListItemDto>> ListAsync(
        string? moderationStatus,
        Guid? experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _feedbacks.ListForStaffAsync(moderationStatus, experienceId, page, pageSize, cancellationToken);
        return new PortalPagedResult<StaffFeedbackListItemDto>
        {
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100),
            TotalCount = total,
            Items = items.Select(MapListItem).ToList()
        };
    }

    public async Task<StaffFeedbackDetailDto?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        var f = await _feedbacks.GetByIdWithVisitAsync(feedbackId, cancellationToken);
        if (f == null)
            return null;

        var item = MapListItem(f);
        int? stopOrder = null;
        if (f.Visit.JourneyId.HasValue)
            stopOrder = await _journeys.GetStopOrderForExperienceOnJourneyAsync(
                f.Visit.JourneyId.Value,
                f.Visit.ExperienceId,
                cancellationToken);

        return new StaffFeedbackDetailDto
        {
            Id = item.Id,
            FeedbackText = item.FeedbackText,
            ModerationStatus = item.ModerationStatus,
            IsFlagged = item.IsFlagged,
            FlaggedReason = item.FlaggedReason,
            CreatedAt = item.CreatedAt,
            VisitId = item.VisitId,
            ExperienceId = item.ExperienceId,
            ExperienceName = item.ExperienceName,
            TravelerId = item.TravelerId,
            TravelerEmail = item.TravelerEmail,
            JourneyId = f.Visit.JourneyId,
            JourneyFeedback = f.Visit.Journey?.JourneyFeedback,
            JourneyFeedbackModerationStatus = f.Visit.Journey?.JourneyFeedbackModerationStatus,
            WaypointStopOrder = stopOrder
        };
    }

    public async Task<PortalPagedResult<StaffJourneyFeedbackListItemDto>> ListJourneyFeedbacksAsync(
        string? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _journeys.ListJourneyFeedbacksForStaffAsync(moderationStatus, page, pageSize, cancellationToken);
        return new PortalPagedResult<StaffJourneyFeedbackListItemDto>
        {
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100),
            TotalCount = total,
            Items = items.Select(j => new StaffJourneyFeedbackListItemDto
            {
                JourneyId = j.Id,
                TravelerId = j.TravelerId,
                TravelerEmail = j.Traveler?.Email,
                JourneyFeedback = j.JourneyFeedback ?? "",
                ModerationStatus = j.JourneyFeedbackModerationStatus,
                UpdatedAt = j.UpdatedAt
            }).ToList()
        };
    }

    public async Task<(bool Ok, string? Error)> ModerateJourneyFeedbackAsync(
        Guid actorUserId,
        Guid journeyId,
        ModerateFeedbackRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var decision = (request.Decision ?? "").Trim().ToLowerInvariant();
        if (decision is not ("approve" or "reject"))
            return (false, "Decision phải là approve hoặc reject.");

        var j = await _journeys.GetBasicByIdAsync(journeyId, cancellationToken);
        if (j == null || string.IsNullOrWhiteSpace(j.JourneyFeedback))
            return (false, "Không tìm thấy chuyến hoặc chưa có feedback chuyến.");

        var old = new { j.JourneyFeedbackModerationStatus };
        var modStatus = decision == "approve"
            ? FeedbackModerationStatuses.Approved
            : FeedbackModerationStatuses.Rejected;

        var ok = await _journeys.TryModerateJourneyFeedbackAsync(journeyId, modStatus, cancellationToken);
        if (!ok)
            return (false, "Không cập nhật được trạng thái.");

        await _audit.LogAsync(
            actorUserId,
            ActionType.StaffJourneyFeedbackModerated,
            nameof(Journey),
            journeyId,
            old,
            new { modStatus, request.Reason },
            ip,
            userAgent,
            cancellationToken);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ModerateAsync(
        Guid actorUserId,
        Guid feedbackId,
        ModerateFeedbackRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var decision = (request.Decision ?? "").Trim().ToLowerInvariant();
        if (decision is not ("approve" or "reject"))
            return (false, "Decision phải là approve hoặc reject.");

        var f = await _feedbacks.GetByIdWithVisitAsync(feedbackId, cancellationToken);
        if (f == null)
            return (false, "Không tìm thấy feedback.");

        var old = new { f.ModerationStatus, f.IsFlagged, f.FlaggedReason };
        string modStatus;
        bool isFlagged;
        string? reason;

        if (decision == "approve")
        {
            modStatus = FeedbackModerationStatuses.Approved;
            isFlagged = false;
            reason = null;
        }
        else
        {
            modStatus = FeedbackModerationStatuses.Rejected;
            isFlagged = true;
            reason = string.IsNullOrWhiteSpace(request.Reason) ? "Từ chối bởi staff" : request.Reason.Trim();
        }

        var ok = await _feedbacks.TryModerateAsync(feedbackId, modStatus, isFlagged, reason, cancellationToken);
        if (!ok)
            return (false, "Không cập nhật được feedback.");

        await _audit.LogAsync(
            actorUserId,
            ActionType.StaffFeedbackModerated,
            nameof(Feedback),
            feedbackId,
            old,
            new { modStatus, isFlagged, reason },
            ip,
            userAgent,
            cancellationToken);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReportUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        ReportPortalUserRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var target = await _users.GetByIdAsync(targetUserId);
        if (target == null || target.DeletedAt != null)
            return (false, "Không tìm thấy user.");

        if (string.Equals(target.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target.Role, AppRoles.Staff, StringComparison.OrdinalIgnoreCase))
            return (false, "Không report/suspend tài khoản staff hoặc admin.");

        var old = new { target.Status };
        target.Status = "suspended";
        target.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(target);

        await _audit.LogAsync(
            actorUserId,
            ActionType.StaffUserReported,
            nameof(User),
            targetUserId,
            old,
            new { request.Reason, request.RelatedFeedbackId },
            ip,
            userAgent,
            cancellationToken);

        return (true, null);
    }

    private static StaffFeedbackListItemDto MapListItem(Feedback f)
    {
        var exp = f.Visit?.Experience;
        var traveler = f.Visit?.Traveler;
        return new StaffFeedbackListItemDto
        {
            Id = f.Id,
            FeedbackText = f.FeedbackText,
            ModerationStatus = f.ModerationStatus,
            IsFlagged = f.IsFlagged,
            FlaggedReason = f.FlaggedReason,
            CreatedAt = f.CreatedAt,
            VisitId = f.VisitId,
            ExperienceId = exp?.Id ?? Guid.Empty,
            ExperienceName = exp?.Name,
            TravelerId = traveler?.Id ?? Guid.Empty,
            TravelerEmail = traveler?.Email
        };
    }
}
