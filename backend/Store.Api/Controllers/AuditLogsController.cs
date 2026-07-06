using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.AuditLogs;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.AuditLogs;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = "OwnerOnly")]
public class AuditLogsController : BaseApiController
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get paged audit logs (Owner only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] string? module,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!IsOwner)
        {
            return ForbiddenResponse();
        }

        var result = await _auditLogService.GetAuditLogsAsync(userId, action, module, fromDate, toDate, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }
}
