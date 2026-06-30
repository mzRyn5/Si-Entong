using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Inventory;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Inventory;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/stock-opnames")]
[Authorize]
public class StockOpnamesController : BaseApiController
{
    private readonly IStockOpnameService _opnameService;

    public StockOpnamesController(IStockOpnameService opnameService)
    {
        _opnameService = opnameService;
    }

    /// <summary>
    /// Get paged stock opname transactions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StockOpnameResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpnames(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.GetAllAsync(fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get stock opname transaction by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockOpnameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpnameById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Opname stok", id);
        }
        return SuccessResponse(result, "Data opname stok berhasil diambil.");
    }

    /// <summary>
    /// Create draft stock opname
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StockOpnameResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOpname(
        [FromBody] CreateStockOpnameRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Draf opname stok berhasil dibuat.");
    }

    /// <summary>
    /// Update draft stock opname
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockOpnameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOpname(
        Guid id,
        [FromBody] UpdateStockOpnameRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Opname stok", id);
        }
        return SuccessResponse(result, "Draf opname stok berhasil diperbarui.");
    }

    /// <summary>
    /// Post a draft stock opname to adjust current stock levels
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<StockOpnameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostOpname(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.PostAsync(id, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Opname stok", id);
        }
        return SuccessResponse(result, "Opname stok berhasil diposting dan stok disesuaikan.");
    }

    /// <summary>
    /// Void a stock opname transaction
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<StockOpnameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoidOpname(
        Guid id,
        [FromBody] string reason = "Voided by user",
        CancellationToken cancellationToken = default)
    {
        var result = await _opnameService.VoidAsync(id, reason, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Opname stok", id);
        }
        return SuccessResponse(result, "Opname stok berhasil dibatalkan.");
    }
}
