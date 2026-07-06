using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Purchases;
using Store.Contracts.Requests.Purchases;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Purchases;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class PurchasesController : BaseApiController
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    /// <summary>
    /// Get all purchases with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PurchaseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? supplierId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _purchaseService.GetAllAsync(
            supplierId, fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get purchase by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pembelian", id);
        }
        return SuccessResponse(result, "Detail pembelian berhasil diambil.");
    }

    /// <summary>
    /// Create new purchase
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Pembelian berhasil dibuat.");
    }

    /// <summary>
    /// Void purchase
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Void(
        Guid id,
        [FromBody] VoidPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.VoidAsync(id, request.Reason, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pembelian", id);
        }
        return SuccessResponse(result, "Pembelian berhasil dibatalkan.");
    }

    /// <summary>
    /// Delete a purchase record
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _purchaseService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Pembelian", id);
        }

        return SuccessResponse<object>(null!, "Pembelian berhasil dihapus.");
    }
}
