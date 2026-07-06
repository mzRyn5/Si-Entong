using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Returns;
using Store.Contracts.Requests.Returns;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Returns;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReturnsController : BaseApiController
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    /// <summary>
    /// Get paged sales return transactions
    /// </summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(PagedResponse<SalesReturnResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesReturns(
        [FromQuery] Guid? saleId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnService.GetAllSalesReturnsAsync(saleId, fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get sales return transaction by ID
    /// </summary>
    [HttpGet("sales/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalesReturnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesReturnById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _returnService.GetSalesReturnByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Retur penjualan", id);
        }
        return SuccessResponse(result, "Data retur penjualan berhasil diambil.");
    }

    /// <summary>
    /// Create new sales return record (refunds customer and restores stock)
    /// </summary>
    [HttpPost("sales")]
    [ProducesResponseType(typeof(ApiResponse<SalesReturnResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSalesReturn(
        [FromBody] CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnService.CreateSalesReturnAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Retur penjualan berhasil dicatat.");
    }

    /// <summary>
    /// Get paged purchase return transactions
    /// </summary>
    [HttpGet("purchases")]
    [ProducesResponseType(typeof(PagedResponse<PurchaseReturnResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseReturns(
        [FromQuery] Guid? purchaseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnService.GetAllPurchaseReturnsAsync(purchaseId, fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get purchase return transaction by ID
    /// </summary>
    [HttpGet("purchases/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchaseReturnById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _returnService.GetPurchaseReturnByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Retur pembelian", id);
        }
        return SuccessResponse(result, "Data retur pembelian berhasil diambil.");
    }

    /// <summary>
    /// Create new purchase return record (returns stock back to supplier)
    /// </summary>
    [HttpPost("purchases")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchaseReturn(
        [FromBody] CreatePurchaseReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnService.CreatePurchaseReturnAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Retur pembelian berhasil dicatat.");
    }
}
