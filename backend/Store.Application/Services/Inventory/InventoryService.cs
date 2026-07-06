using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Inventory;
using Store.Domain.Entities;
using Store.Application.Abstractions.Repositories;


namespace Store.Application.Services.Inventory;

public interface IInventoryService
{
    Task<PagedResponse<StockMovementResponse>> GetStockMovementsAsync(
        Guid? productId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<StockSummaryResponse>> GetStockSummaryAsync(
        string? search,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public class InventoryService : IInventoryService
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductRepository _productRepository;

    public InventoryService(
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _productRepository = productRepository;
    }

    public async Task<PagedResponse<StockMovementResponse>> GetStockMovementsAsync(
        Guid? productId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var movements = await _stockMovementRepository.GetAllAsync(
            productId, fromDate, toDate, page, pageSize, cancellationToken);

        var totalCount = await _stockMovementRepository.GetTotalCountAsync(productId, fromDate, toDate, cancellationToken);

        var responses = movements.Select(MapToResponse).ToList();

        return new PagedResponse<StockMovementResponse>
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

    public async Task<PagedResponse<StockSummaryResponse>> GetStockSummaryAsync(
        string? search,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(
            search, categoryId, true, null, page, pageSize, cancellationToken);

        var totalCount = await _productRepository.GetTotalCountAsync(search, categoryId, true, cancellationToken);

        var responses = products.Select(p => new StockSummaryResponse
        {
            ProductId = p.Id,
            Sku = p.Sku,
            ProductName = p.Name,
            CategoryName = p.Category?.Name ?? "",
            UnitName = p.Unit?.Name ?? "",
            CurrentStock = p.CurrentStock,
            LowStockThreshold = p.LowStockThreshold,
            StockValue = p.StockValue,
            SellingPrice = p.SellingPrice
        }).ToList();

        return new PagedResponse<StockSummaryResponse>
        {
            Success = true,
            Message = "Ringkasan stok berhasil diambil.",
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

    private static StockMovementResponse MapToResponse(StockMovement movement) => new()
    {
        Id = movement.Id,
        ProductId = movement.ProductId,
        Sku = movement.Product?.Sku ?? "",
        ProductName = movement.Product?.Name ?? "",
        MovementDate = movement.MovementDate,
        MovementType = movement.MovementType.ToString(),
        QuantityBefore = movement.QuantityBefore,
        QuantityChange = movement.QuantityChange,
        QuantityAfter = movement.QuantityAfter,
        ReferenceType = movement.ReferenceType,
        ReferenceId = movement.ReferenceId?.ToString(),
        ReferenceNumber = movement.ReferenceId?.ToString(),
        Notes = movement.Notes
    };
}
