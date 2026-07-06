using Store.Application.Abstractions.Repositories;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.AuditLogs;
using Store.Domain.Entities;

namespace Store.Application.Services.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponse>> GetAuditLogsAsync(Guid? userId, string? action, string? module, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
}

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsAsync(
        Guid? userId, string? action, string? module, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogRepository.GetAllAsync(userId, action, module, fromDate, toDate, page, pageSize, cancellationToken);
        var totalCount = await _auditLogRepository.GetTotalCountAsync(userId, action, module, cancellationToken);

        var responses = logs.Select(MapToResponse).ToList();

        return new PagedResponse<AuditLogResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    private AuditLogResponse MapToResponse(AuditLog log)
    {
        return new AuditLogResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.User?.Name ?? log.User?.Username ?? "System",
            Action = log.Action,
            Module = log.Module,
            CreatedAt = log.CreatedAt
        };
    }
}
