using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Payables;
using Store.Contracts.Requests.Payables;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Payables;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class PayablesController : BaseApiController
{
    private readonly IPayableService _payableService;

    public PayablesController(IPayableService payableService)
    {
        _payableService = payableService;
    }

    /// <summary>
    /// Get all payables with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PayableListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _payableService.GetAllAsync(supplierId, status, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get payable by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _payableService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Hutang", id);
        }
        return SuccessResponse(result, "Detail hutang berhasil diambil.");
    }

    /// <summary>
    /// Record payment on payable
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(ApiResponse<PayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPayment(
        Guid id,
        [FromBody] RecordPayablePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payableService.RecordPaymentAsync(id, request, CurrentUserId, cancellationToken);
        return SuccessResponse(result, "Pembayaran hutang berhasil dicatat.");
    }

    /// <summary>
    /// Cancel a payment on a payable
    /// </summary>
    [HttpDelete("{id:guid}/payments/{paymentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayment(
        Guid id,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result = await _payableService.CancelPaymentAsync(id, paymentId, CurrentUserId, cancellationToken);
        return SuccessResponse(result, "Pembayaran hutang berhasil dibatalkan.");
    }

    /// <summary>
    /// Delete a payable record
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _payableService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Hutang", id);
        }

        return SuccessResponse<object>(null!, "Hutang berhasil dihapus.");
    }
}
