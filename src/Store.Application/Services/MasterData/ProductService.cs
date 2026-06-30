using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Products;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Products;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.MasterData;

public interface IProductService
{
    Task<PagedResponse<ProductListItemResponse>> GetAllAsync(
        string? search, Guid? categoryId, bool? isActive, bool? isLowStock,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListItemResponse>> GetAllForPosAsync(string? search, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListItemResponse>> GetLowStockAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProductService(
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository)
    {
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<ProductListItemResponse>> GetAllAsync(
        string? search, Guid? categoryId, bool? isActive, bool? isLowStock,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(search, categoryId, isActive, isLowStock, page, pageSize, cancellationToken);
        var totalCount = await _productRepository.GetTotalCountAsync(search, categoryId, isActive, cancellationToken);

        var responses = products.Select(MapToListItemResponse).ToList();

        return new PagedResponse<ProductListItemResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<IEnumerable<ProductListItemResponse>> GetAllForPosAsync(string? search, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllForPosAsync(search, cancellationToken);
        return products.Select(MapToListItemResponse);
    }

    public async Task<IEnumerable<ProductListItemResponse>> GetLowStockAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetLowStockProductsAsync(page, pageSize, cancellationToken);
        return products.Select(MapToListItemResponse);
    }

    public async Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetLowStockCountAsync(cancellationToken);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByBarcodeAsync(barcode, cancellationToken);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (await _productRepository.SkuExistsAsync(request.Sku, null, cancellationToken))
        {
            throw new BusinessRuleException("SKU sudah digunakan.", "DUPLICATE_DATA");
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            await _productRepository.BarcodeExistsAsync(request.Barcode, null, cancellationToken))
        {
            throw new BusinessRuleException("Barcode sudah digunakan.", "DUPLICATE_DATA");
        }

        var product = new Product
        {
            Sku = request.Sku,
            Barcode = request.Barcode,
            Name = request.Name,
            CategoryId = request.CategoryId,
            UnitId = request.UnitId,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            LowStockThreshold = request.LowStockThreshold,
            CurrentStock = request.InitialStock,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _productRepository.AddAsync(product, cancellationToken);

        // If initial stock is provided, create opening stock movement
        if (request.InitialStock > 0)
        {
            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ProductId = created.Id,
                MovementDate = DateTime.UtcNow,
                MovementType = Domain.Enums.StockMovementType.OpeningStock,
                QuantityBefore = 0,
                QuantityChange = request.InitialStock,
                QuantityAfter = request.InitialStock,
                Notes = "Stok awal",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Product",
            EntityId = created.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Reload with includes
        var result = await _productRepository.GetByIdAsync(created.Id, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null) return null;

        if (await _productRepository.SkuExistsAsync(request.Sku, id, cancellationToken))
        {
            throw new BusinessRuleException("SKU sudah digunakan.", "DUPLICATE_DATA");
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            await _productRepository.BarcodeExistsAsync(request.Barcode, id, cancellationToken))
        {
            throw new BusinessRuleException("Barcode sudah digunakan.", "DUPLICATE_DATA");
        }

        // Track price changes for audit
        var priceChanged = product.SellingPrice != request.SellingPrice || product.PurchasePrice != request.PurchasePrice;

        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.UnitId = request.UnitId;
        product.PurchasePrice = request.PurchasePrice;
        product.SellingPrice = request.SellingPrice;
        product.LowStockThreshold = request.LowStockThreshold;
        product.IsActive = request.IsActive;
        product.UpdatedBy = updatedBy;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = priceChanged ? "UpdatePrice" : "Update",
            EntityName = "Product",
            EntityId = product.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Reload with includes
        var result = await _productRepository.GetByIdAsync(id, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null) return false;

        product.IsActive = false;
        product.UpdatedBy = deletedBy;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Deactivate",
            EntityName = "Product",
            EntityId = product.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Barcode = product.Barcode,
        Name = product.Name,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? "",
        UnitId = product.UnitId,
        UnitName = product.Unit?.Name ?? "",
        PurchasePrice = product.PurchasePrice,
        SellingPrice = product.SellingPrice,
        CurrentStock = product.CurrentStock,
        LowStockThreshold = product.LowStockThreshold,
        IsLowStock = product.IsLowStock,
        IsOutOfStock = product.IsOutOfStock,
        IsActive = product.IsActive
    };

    private static ProductListItemResponse MapToListItemResponse(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Barcode = product.Barcode,
        Name = product.Name,
        CategoryName = product.Category?.Name ?? "",
        UnitName = product.Unit?.Name ?? "",
        PurchasePrice = product.PurchasePrice,
        SellingPrice = product.SellingPrice,
        CurrentStock = product.CurrentStock,
        LowStockThreshold = product.LowStockThreshold,
        IsLowStock = product.IsLowStock,
        IsOutOfStock = product.IsOutOfStock,
        IsActive = product.IsActive
    };
}
