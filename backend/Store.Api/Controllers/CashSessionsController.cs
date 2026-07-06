using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Sales;
using Store.Contracts.Requests.CashSessions;
using Store.Contracts.Responses.CashSessions;
using Store.Contracts.Responses.Common;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/cash-sessions")]
[Authorize(Policy = "OwnerOrAdmin")]
public class CashSessionsController : BaseApiController
{
    private readonly ICashSessionService _cashSessionService;

    public CashSessionsController(ICashSessionService cashSessionService)
    {
        _cashSessionService = cashSessionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CashSessionListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? cashierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _cashSessionService.GetAllAsync(
            cashierId, fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<CashSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _cashSessionService.GetActiveAsync(CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Sesi kasir aktif", CurrentUserId);
        }
        return SuccessResponse(result, "Sesi kasir aktif berhasil diambil.");
    }

    [HttpPost("open")]
    [ProducesResponseType(typeof(ApiResponse<CashSessionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(
        [FromBody] OpenCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cashSessionService.OpenAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Sesi kasir berhasil dibuka.");
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(ApiResponse<CashSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Close(
        Guid id,
        [FromBody] CloseCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cashSessionService.CloseAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Sesi kasir", id);
        }
        return SuccessResponse(result, "Sesi kasir berhasil ditutup.");
    }
}
