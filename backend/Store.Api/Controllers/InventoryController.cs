using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Inventory;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Inventory;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;
    private readonly IStockAdjustmentService _stockAdjustmentService;

    public InventoryController(
        IInventoryService inventoryService,
        IStockAdjustmentService stockAdjustmentService)
    {
        _inventoryService = inventoryService;
        _stockAdjustmentService = stockAdjustmentService;
    }

    /// <summary>
    /// Get stock summary for all products
    /// </summary>
    [HttpGet("stock-summary")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StockSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockSummary(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetStockSummaryAsync(search, categoryId, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get stock movement history
    /// </summary>
    [HttpGet("stock-movements")]
    [ProducesResponseType(typeof(PagedResponse<StockMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? productId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetStockMovementsAsync(
            productId, fromDate, toDate, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    #region Stock Adjustments

    /// <summary>
    /// Get all stock adjustments with pagination and filtering
    /// </summary>
    [HttpGet("stock-adjustments")]
    [ProducesResponseType(typeof(PagedResponse<StockAdjustmentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockAdjustments(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockAdjustmentService.GetAllAsync(
            fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get stock adjustment by ID
    /// </summary>
    [HttpGet("stock-adjustments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockAdjustmentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Koreksi stok", id);
        }
        return SuccessResponse(result, "Detail koreksi stok berhasil diambil.");
    }

    /// <summary>
    /// Create new stock adjustment (draft)
    /// </summary>
    [HttpPost("stock-adjustments")]
    [ProducesResponseType(typeof(ApiResponse<CreateStockAdjustmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStockAdjustment(
        [FromBody] CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Koreksi stok berhasil dibuat.");
    }

    /// <summary>
    /// Update stock adjustment (draft only)
    /// </summary>
    [HttpPut("stock-adjustments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStockAdjustment(
        Guid id,
        [FromBody] UpdateStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Koreksi stok", id);
        }
        return SuccessResponse(result, "Koreksi stok berhasil diperbarui.");
    }

    /// <summary>
    /// Post stock adjustment (apply to stock)
    /// </summary>
    [HttpPost("stock-adjustments/{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PostStockAdjustment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.PostAsync(id, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Koreksi stok", id);
        }
        return SuccessResponse(result, "Koreksi stok berhasil diposting.");
    }

    /// <summary>
    /// Void stock adjustment
    /// </summary>
    [HttpPost("stock-adjustments/{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VoidStockAdjustment(
        Guid id,
        [FromBody] VoidStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.VoidAsync(id, request.Reason, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Koreksi stok", id);
        }
        return SuccessResponse(result, "Koreksi stok berhasil dibatalkan.");
    }

    #endregion
}
