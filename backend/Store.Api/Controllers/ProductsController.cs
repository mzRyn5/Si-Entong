using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.MasterData;
using Store.Contracts.Requests.Products;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Products;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "OwnerOrAdmin")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Get all products with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLowStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetAllAsync(search, categoryId, isActive, isLowStock, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get products with low stock
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetLowStockAsync(page, pageSize, cancellationToken);
        var total = await _productService.GetLowStockCountAsync(cancellationToken);
        
        return PagedResponse(new PagedResponse<ProductListItemResponse>
        {
            Data = result,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        });
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Produk", id);
        }
        return SuccessResponse(result, "Data produk berhasil diambil.");
    }

    /// <summary>
    /// Get product by barcode
    /// </summary>
    [HttpGet("by-barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByBarcodeAsync(barcode, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Produk", barcode);
        }
        return SuccessResponse(result, "Data produk berhasil diambil.");
    }

    /// <summary>
    /// Create new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Produk berhasil dibuat.");
    }

    /// <summary>
    /// Update existing product
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Produk", id);
        }
        return SuccessResponse(result, "Produk berhasil diperbarui.");
    }

    /// <summary>
    /// Soft delete (deactivate) product
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(id, CurrentUserId, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("Produk", id);
        }
        return SuccessResponse("Produk berhasil dinonaktifkan.");
    }
}
