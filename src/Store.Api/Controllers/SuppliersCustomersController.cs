using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.MasterData;
using Store.Contracts.Requests.Suppliers;
using Store.Contracts.Requests.Customers;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Suppliers;
using Store.Contracts.Responses.Customers;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class SuppliersController : BaseApiController
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Get all suppliers with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SupplierResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Supplier", id);
        }
        return SuccessResponse(result, "Data supplier berhasil diambil.");
    }

    /// <summary>
    /// Create new supplier
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await _supplierService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Supplier berhasil dibuat.");
    }

    /// <summary>
    /// Update existing supplier
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await _supplierService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Supplier", id);
        }
        return SuccessResponse(result, "Supplier berhasil diperbarui.");
    }

    /// <summary>
    /// Soft delete (deactivate) supplier
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _supplierService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("Supplier", id);
        }
        return SuccessResponse("Supplier berhasil dinonaktifkan.");
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pelanggan", id);
        }
        return SuccessResponse(result, "Data pelanggan berhasil diambil.");
    }

    /// <summary>
    /// Create new customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Pelanggan berhasil dibuat.");
    }

    /// <summary>
    /// Update existing customer
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pelanggan", id);
        }
        return SuccessResponse(result, "Pelanggan berhasil diperbarui.");
    }

    /// <summary>
    /// Soft delete (deactivate) customer
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("Pelanggan", id);
        }
        return SuccessResponse("Pelanggan berhasil dinonaktifkan.");
    }
}
