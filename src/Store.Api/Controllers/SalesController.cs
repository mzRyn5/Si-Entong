using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Sales;
using Store.Contracts.Requests.Sales;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Sales;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class SalesController : BaseApiController
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SaleListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? cashierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _saleService.GetAllAsync(
            cashierId, fromDate, toDate, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _saleService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Penjualan", id);
        }

        return SuccessResponse(result, "Detail penjualan berhasil diambil.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _saleService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Penjualan berhasil dibuat.");
    }

    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Void(
        Guid id,
        [FromBody] VoidSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _saleService.VoidAsync(id, request.Reason, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Penjualan", id);
        }

        return SuccessResponse(result, "Penjualan berhasil dibatalkan.");
    }

    [HttpGet("{id:guid}/receipt")]
    [ProducesResponseType(typeof(ApiResponse<SaleReceiptResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await _saleService.GetReceiptAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Penjualan", id);
        }

        return SuccessResponse(result, "Data struk berhasil diambil.");
    }

    /// <summary>
    /// Delete a sale record
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _saleService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Penjualan", id);
        }

        return SuccessResponse<object>(null!, "Penjualan berhasil dihapus.");
    }
}
