using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.CashMovements;
using Store.Contracts.Requests.CashMovements;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.CashMovements;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CashMovementsController : BaseApiController
{
    private readonly ICashMovementService _cashMovementService;

    public CashMovementsController(ICashMovementService cashMovementService)
    {
        _cashMovementService = cashMovementService;
    }

    /// <summary>
    /// Get paged cash movements
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CashMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? cashSessionId,
        [FromQuery] string? type,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _cashMovementService.GetAllMovementsAsync(cashSessionId, type, fromDate, toDate, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Record a cash movement (In/Out)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CashMovementResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMovement(
        [FromBody] CreateCashMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _cashMovementService.CreateMovementAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Mutasi kas berhasil dicatat.");
    }
}
