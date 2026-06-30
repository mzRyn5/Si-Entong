using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Receivables;
using Store.Contracts.Requests.Receivables;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Receivables;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class ReceivablesController : BaseApiController
{
    private readonly IReceivableService _receivableService;

    public ReceivablesController(IReceivableService receivableService)
    {
        _receivableService = receivableService;
    }

    /// <summary>
    /// Get all receivables with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ReceivableListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _receivableService.GetAllAsync(customerId, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get receivable by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReceivableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _receivableService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Piutang", id);
        }
        return SuccessResponse(result, "Detail piutang berhasil diambil.");
    }

    /// <summary>
    /// Record payment on receivable
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(ApiResponse<ReceivableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPayment(
        Guid id,
        [FromBody] RecordReceivablePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _receivableService.RecordPaymentAsync(id, request, CurrentUserId, cancellationToken);
        return SuccessResponse(result, "Pembayaran piutang berhasil dicatat.");
    }

    /// <summary>
    /// Delete a receivable record
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _receivableService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Piutang", id);
        }

        return SuccessResponse<object>(null!, "Piutang berhasil dihapus.");
    }
}
