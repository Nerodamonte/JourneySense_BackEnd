using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        if (log.Id == Guid.Empty)
            log.Id = Guid.NewGuid();
        log.CreatedAt ??= DateTime.UtcNow;
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? actorUserId,
        ActionType? actionType,
        string? entityType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _context.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (actorUserId.HasValue)
            q = q.Where(a => a.UserId == actorUserId.Value);

        if (actionType.HasValue)
            q = q.Where(a => a.ActionType == actionType.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var et = entityType.Trim();
            q = q.Where(a => a.EntityType == et);
        }

        if (fromUtc.HasValue)
            q = q.Where(a => a.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            q = q.Where(a => a.CreatedAt <= toUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
