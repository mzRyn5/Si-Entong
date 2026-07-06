using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.MasterData;
using Store.Contracts.Requests.Units;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Units;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class UnitsController : BaseApiController
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UnitResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _unitService.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    [HttpGet("dropdown")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UnitResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllForDropdown(CancellationToken cancellationToken = default)
    {
        var result = await _unitService.GetAllForDropdownAsync(cancellationToken);
        return SuccessResponse(result, "Data satuan dropdown berhasil diambil.");
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _unitService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Satuan", id);
        }
        return SuccessResponse(result, "Data satuan berhasil diambil.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _unitService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Satuan berhasil dibuat.");
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _unitService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Satuan", id);
        }
        return SuccessResponse(result, "Satuan berhasil diperbarui.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _unitService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("Satuan", id);
        }
        return SuccessResponse("Satuan berhasil dinonaktifkan.");
    }
}
