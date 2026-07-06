using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Returns;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Returns;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.Returns;

public interface IReturnService
{
    Task<PagedResponse<SalesReturnResponse>> GetAllSalesReturnsAsync(Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SalesReturnResponse?> GetSalesReturnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesReturnResponse> CreateSalesReturnAsync(CreateSalesReturnRequest request, Guid createdBy, CancellationToken cancellationToken = default);

    Task<PagedResponse<PurchaseReturnResponse>> GetAllPurchaseReturnsAsync(Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PurchaseReturnResponse?> GetPurchaseReturnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReturnResponse> CreatePurchaseReturnAsync(CreatePurchaseReturnRequest request, Guid createdBy, CancellationToken cancellationToken = default);
}

public class ReturnService : IReturnService
{
    private readonly ISalesReturnRepository _salesReturnRepository;
    private readonly IPurchaseReturnRepository _purchaseReturnRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ReturnService(
        ISalesReturnRepository salesReturnRepository,
        IPurchaseReturnRepository purchaseReturnRepository,
        ISaleRepository saleRepository,
        IPurchaseRepository purchaseRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository)
    {
        _salesReturnRepository = salesReturnRepository;
        _purchaseReturnRepository = purchaseReturnRepository;
        _saleRepository = saleRepository;
        _purchaseRepository = purchaseRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<SalesReturnResponse>> GetAllSalesReturnsAsync(
        Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var returns = await _salesReturnRepository.GetAllAsync(saleId, fromDate, toDate, status, page, pageSize, cancellationToken);
        var totalCount = await _salesReturnRepository.GetTotalCountAsync(saleId, fromDate, toDate, status, cancellationToken);

        var responses = returns.Select(MapToSalesResponse).ToList();

        return new PagedResponse<SalesReturnResponse>
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

    public async Task<SalesReturnResponse?> GetSalesReturnByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var salesReturn = await _salesReturnRepository.GetByIdAsync(id, cancellationToken);
        return salesReturn == null ? null : MapToSalesResponse(salesReturn);
    }

    public async Task<SalesReturnResponse> CreateSalesReturnAsync(
        CreateSalesReturnRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale == null)
        {
            throw new BusinessRuleException("Transaksi penjualan tidak ditemukan.", "SALE_NOT_FOUND");
        }

        if (sale.Status == Domain.Enums.TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Tidak dapat meretur transaksi penjualan yang sudah dibatalkan/void.", "INVALID_TRANSACTION_STATUS");
        }

        var returnNumber = await _salesReturnRepository.GenerateReturnNumberAsync(cancellationToken);
        var salesReturn = new SalesReturn
        {
            ReturnNumber = returnNumber,
            SaleId = request.SaleId,
            ReturnDate = DateTimeOffset.UtcNow,
            Reason = request.Reason,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalRefund = 0;

        // Fetch previous returns for this sale to validate quantities
        var previousReturns = await _salesReturnRepository.GetAllAsync(request.SaleId, null, null, null, 1, 1000, cancellationToken);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                throw new BusinessRuleException("Jumlah kuantitas retur harus lebih besar dari 0.", "INVALID_QUANTITY");
            }

            var saleItem = sale.Items.FirstOrDefault(i => i.ProductId == itemReq.ProductId);
            if (saleItem == null)
            {
                throw new BusinessRuleException($"Produk '{itemReq.ProductId}' tidak ditemukan dalam transaksi penjualan asal.", "ITEM_NOT_FOUND");
            }

            // Calculate already returned quantity
            int alreadyReturnedQty = previousReturns
                .SelectMany(r => r.Items)
                .Where(i => i.ProductId == itemReq.ProductId)
                .Sum(i => i.Quantity);

            int allowedQty = saleItem.Quantity - alreadyReturnedQty;
            if (itemReq.Quantity > allowedQty)
            {
                throw new BusinessRuleException($"Jumlah retur untuk produk '{saleItem.ProductSnapshotName}' ({itemReq.Quantity}) melebihi kuantitas yang dapat diretur ({allowedQty}).", "EXCEEDED_RETURN_LIMIT");
            }

            if (!productsDict.TryGetValue(itemReq.ProductId, out var product))
            {
                throw new BusinessRuleException($"Produk dengan ID '{itemReq.ProductId}' tidak ditemukan.", "PRODUCT_NOT_FOUND");
            }

            var totalPrice = itemReq.Quantity * saleItem.UnitPrice;

            var returnItem = new SalesReturnItem
            {
                ProductId = itemReq.ProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = saleItem.UnitPrice,
                TotalPrice = totalPrice,
                Product = product
            };

            salesReturn.Items.Add(returnItem);
            totalRefund += totalPrice;

            // Update Stock
            int oldStock = product.CurrentStock;
            int newStock = oldStock + itemReq.Quantity;
            product.CurrentStock = newStock;
            await _productRepository.UpdateAsync(product, cancellationToken);

            // Stock Movement
            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ProductId = itemReq.ProductId,
                MovementDate = DateTime.UtcNow,
                MovementType = Domain.Enums.StockMovementType.SalesReturn,
                QuantityBefore = oldStock,
                QuantityChange = itemReq.Quantity,
                QuantityAfter = newStock,
                ReferenceType = "SalesReturn",
                ReferenceId = salesReturn.Id,
                Notes = $"Retur Penjualan: {salesReturn.ReturnNumber}",
                CreatedBy = createdBy
            }, cancellationToken);
        }

        salesReturn.TotalAmount = totalRefund;
        var created = await _salesReturnRepository.AddAsync(salesReturn, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "SalesReturn",
            EntityId = created.Id,
            Module = "Returns",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        created.Sale = sale;
        return MapToSalesResponse(created);
    }

    public async Task<PagedResponse<PurchaseReturnResponse>> GetAllPurchaseReturnsAsync(
        Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var returns = await _purchaseReturnRepository.GetAllAsync(purchaseId, fromDate, toDate, status, page, pageSize, cancellationToken);
        var totalCount = await _purchaseReturnRepository.GetTotalCountAsync(purchaseId, fromDate, toDate, status, cancellationToken);

        var responses = returns.Select(MapToPurchaseResponse).ToList();

        return new PagedResponse<PurchaseReturnResponse>
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

    public async Task<PurchaseReturnResponse?> GetPurchaseReturnByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
        return purchaseReturn == null ? null : MapToPurchaseResponse(purchaseReturn);
    }

    public async Task<PurchaseReturnResponse> CreatePurchaseReturnAsync(
        CreatePurchaseReturnRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId, cancellationToken);
        if (purchase == null)
        {
            throw new BusinessRuleException("Transaksi pembelian tidak ditemukan.", "PURCHASE_NOT_FOUND");
        }

        if (purchase.Status == Domain.Enums.TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Tidak dapat meretur transaksi pembelian yang sudah dibatalkan/void.", "INVALID_TRANSACTION_STATUS");
        }

        var returnNumber = await _purchaseReturnRepository.GenerateReturnNumberAsync(cancellationToken);
        var purchaseReturn = new PurchaseReturn
        {
            ReturnNumber = returnNumber,
            PurchaseId = request.PurchaseId,
            ReturnDate = DateTimeOffset.UtcNow,
            Reason = request.Reason,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalRefund = 0;

        // Fetch previous returns for this purchase to validate quantities
        var previousReturns = await _purchaseReturnRepository.GetAllAsync(request.PurchaseId, null, null, null, 1, 1000, cancellationToken);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                throw new BusinessRuleException("Jumlah kuantitas retur harus lebih besar dari 0.", "INVALID_QUANTITY");
            }

            var purchaseItem = purchase.Items.FirstOrDefault(i => i.ProductId == itemReq.ProductId);
            if (purchaseItem == null)
            {
                throw new BusinessRuleException($"Produk '{itemReq.ProductId}' tidak ditemukan dalam transaksi pembelian asal.", "ITEM_NOT_FOUND");
            }

            // Calculate already returned quantity
            int alreadyReturnedQty = previousReturns
                .SelectMany(r => r.Items)
                .Where(i => i.ProductId == itemReq.ProductId)
                .Sum(i => i.Quantity);

            int allowedQty = purchaseItem.Quantity - alreadyReturnedQty;
            if (itemReq.Quantity > allowedQty)
            {
                throw new BusinessRuleException($"Jumlah retur untuk produk '{purchaseItem.ProductSnapshotName}' ({itemReq.Quantity}) melebihi kuantitas yang dapat diretur ({allowedQty}).", "EXCEEDED_RETURN_LIMIT");
            }

            if (!productsDict.TryGetValue(itemReq.ProductId, out var product))
            {
                throw new BusinessRuleException($"Produk dengan ID '{itemReq.ProductId}' tidak ditemukan.", "PRODUCT_NOT_FOUND");
            }

            var totalPrice = itemReq.Quantity * purchaseItem.UnitPrice;

            var returnItem = new PurchaseReturnItem
            {
                ProductId = itemReq.ProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = purchaseItem.UnitPrice,
                TotalPrice = totalPrice,
                Product = product
            };

            purchaseReturn.Items.Add(returnItem);
            totalRefund += totalPrice;

            // Update Stock
            int oldStock = product.CurrentStock;
            int newStock = oldStock - itemReq.Quantity;
            product.CurrentStock = newStock;
            await _productRepository.UpdateAsync(product, cancellationToken);

            // Stock Movement
            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ProductId = itemReq.ProductId,
                MovementDate = DateTime.UtcNow,
                MovementType = Domain.Enums.StockMovementType.PurchaseReturn,
                QuantityBefore = oldStock,
                QuantityChange = -itemReq.Quantity,
                QuantityAfter = newStock,
                ReferenceType = "PurchaseReturn",
                ReferenceId = purchaseReturn.Id,
                Notes = $"Retur Pembelian: {purchaseReturn.ReturnNumber}",
                CreatedBy = createdBy
            }, cancellationToken);
        }

        purchaseReturn.TotalAmount = totalRefund;
        var created = await _purchaseReturnRepository.AddAsync(purchaseReturn, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "PurchaseReturn",
            EntityId = created.Id,
            Module = "Returns",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        created.Purchase = purchase;
        return MapToPurchaseResponse(created);
    }

    private SalesReturnResponse MapToSalesResponse(SalesReturn salesReturn)
    {
        return new SalesReturnResponse
        {
            Id = salesReturn.Id,
            ReturnNumber = salesReturn.ReturnNumber,
            SaleId = salesReturn.SaleId,
            SaleNumber = salesReturn.Sale?.SaleNumber ?? string.Empty,
            ReturnDate = salesReturn.ReturnDate,
            Reason = salesReturn.Reason,
            TotalAmount = salesReturn.TotalAmount,
            TotalRefundAmount = salesReturn.TotalAmount,
            Status = "Completed",
            Items = salesReturn.Items.Select(i => new SalesReturnItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                RefundAmount = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }

    private PurchaseReturnResponse MapToPurchaseResponse(PurchaseReturn purchaseReturn)
    {
        return new PurchaseReturnResponse
        {
            Id = purchaseReturn.Id,
            ReturnNumber = purchaseReturn.ReturnNumber,
            PurchaseId = purchaseReturn.PurchaseId,
            PurchaseNumber = purchaseReturn.Purchase?.PurchaseNumber ?? string.Empty,
            ReturnDate = purchaseReturn.ReturnDate,
            Reason = purchaseReturn.Reason,
            TotalAmount = purchaseReturn.TotalAmount,
            Status = "Completed",
            Items = purchaseReturn.Items.Select(i => new PurchaseReturnItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Amount = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
