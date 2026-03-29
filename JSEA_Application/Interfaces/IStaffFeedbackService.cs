using JSEA_Application.DTOs.Portal;
using System.Net;

namespace JSEA_Application.Interfaces;

public interface IStaffFeedbackService
{
    Task<PortalPagedResult<StaffFeedbackListItemDto>> ListAsync(
        string? moderationStatus,
        Guid? experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StaffFeedbackDetailDto?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> ModerateAsync(
        Guid actorUserId,
        Guid feedbackId,
        ModerateFeedbackRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<PortalPagedResult<StaffJourneyFeedbackListItemDto>> ListJourneyFeedbacksAsync(
        string? moderationStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> ModerateJourneyFeedbackAsync(
        Guid actorUserId,
        Guid journeyId,
        ModerateFeedbackRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> ReportUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        ReportPortalUserRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
