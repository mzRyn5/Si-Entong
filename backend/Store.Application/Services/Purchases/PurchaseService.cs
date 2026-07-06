using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Purchases;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Purchases;
using Store.Contracts.Responses.Suppliers;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using Store.SharedKernel;

namespace Store.Application.Services.Purchases;

public interface IPurchaseService
{
    Task<PagedResponse<PurchaseListItemResponse>> GetAllAsync(
        Guid? supplierId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PurchaseResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PurchaseResponse> CreateAsync(
        CreatePurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PurchaseResponse?> VoidAsync(
        Guid id,
        string reason,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStoreSettingsRepository _storeSettingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPayableRepository _payableRepository;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IProductRepository productRepository,
        ISupplierRepository supplierRepository,
        IStockMovementRepository stockMovementRepository,
        IStoreSettingsRepository storeSettingsRepository,
        IAuditLogRepository auditLogRepository,
        IPayableRepository payableRepository)
    {
        _purchaseRepository = purchaseRepository;
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _stockMovementRepository = stockMovementRepository;
        _storeSettingsRepository = storeSettingsRepository;
        _auditLogRepository = auditLogRepository;
        _payableRepository = payableRepository;
    }

    public async Task<PagedResponse<PurchaseListItemResponse>> GetAllAsync(
        Guid? supplierId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var purchases = await _purchaseRepository.GetAllAsync(
            supplierId, fromDate, toDate, status, page, pageSize, cancellationToken);

        var totalCount = await _purchaseRepository.GetTotalCountAsync(supplierId, fromDate, toDate, status, cancellationToken);

        var responses = purchases.Select(p => new PurchaseListItemResponse
        {
            Id = p.Id,
            PurchaseNumber = p.PurchaseNumber,
            PurchaseDate = p.PurchaseDate,
            SupplierName = p.Supplier?.Name ?? "",
            TotalAmount = p.TotalAmount,
            Status = p.Status.ToString(),
            PaymentStatus = p.PaymentStatus.ToString()
        }).ToList();

        return new PagedResponse<PurchaseListItemResponse>
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

    public async Task<PurchaseResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (purchase == null) return null;

        return MapToResponse(purchase);
    }

    public async Task<PurchaseResponse> CreateAsync(
        CreatePurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Validate supplier
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier == null)
        {
            throw new NotFoundException("Supplier", request.SupplierId.ToString());
        }

        if (!supplier.IsActive)
        {
            throw new BusinessRuleException("Supplier tidak aktif.");
        }

        // Validate items
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("Minimal 1 item pembelian.");
        }

        // Parse payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            paymentMethod = PaymentMethod.Cash;
        }

        // Generate purchase number
        var purchaseNumber = await GeneratePurchaseNumberAsync(cancellationToken);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        // Calculate totals
        var items = new List<PurchaseItem>();
        decimal subtotal = 0;

        foreach (var item in request.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product))
            {
                throw new NotFoundException("Produk", item.ProductId.ToString());
            }

            if (!product.IsActive)
            {
                throw new BusinessRuleException($"Produk '{product.Name}' tidak aktif.");
            }

            if (item.Quantity <= 0)
            {
                throw new BusinessRuleException("Quantity harus lebih dari 0.");
            }

            if (item.UnitPrice < 0)
            {
                throw new BusinessRuleException("Harga beli tidak boleh negatif.");
            }

            var itemSubtotal = item.Quantity * item.UnitPrice;

            items.Add(new PurchaseItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = itemSubtotal,
                ProductSnapshotName = product.Name,
                ProductSnapshotSku = product.Sku
            });

            subtotal += itemSubtotal;
        }

        var totalAmount = subtotal - request.DiscountAmount;

        // Parse payment status
        if (!Enum.TryParse<PaymentStatus>(request.PaymentStatus, true, out var paymentStatus))
        {
            paymentStatus = PaymentStatus.Paid;
        }

        if (paymentStatus == PaymentStatus.Unpaid && request.AmountPaid > 0)
        {
            paymentStatus = PaymentStatus.Partial;
        }
        else if (paymentStatus == PaymentStatus.Partial && request.AmountPaid == 0)
        {
            paymentStatus = PaymentStatus.Unpaid;
        }

        // Create purchase
        var purchase = new Purchase
        {
            PurchaseNumber = purchaseNumber,
            PurchaseDate = request.PurchaseDate.UtcDateTime,
            SupplierId = supplier.Id,
            Subtotal = subtotal,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentStatus,
            Status = TransactionStatus.Posted,
            Notes = request.Notes,
            CreatedBy = userId
        };

        foreach (var item in items)
        {
            purchase.Items.Add(item);
        }

        var created = await _purchaseRepository.AddAsync(purchase, cancellationToken);

        // Generate Payable record if Unpaid or Partial
        if (created.PaymentStatus == PaymentStatus.Unpaid || created.PaymentStatus == PaymentStatus.Partial)
        {
            var payableNumber = await GeneratePayableNumberAsync(cancellationToken);
            
            var initialPaid = created.PaymentStatus == PaymentStatus.Partial ? request.AmountPaid : 0;
            if (initialPaid > created.TotalAmount)
            {
                initialPaid = created.TotalAmount;
            }

            var payable = new Payable
            {
                PayableNumber = payableNumber,
                PurchaseId = created.Id,
                SupplierId = created.SupplierId,
                TotalAmount = created.TotalAmount,
                PaidAmount = initialPaid,
                RemainingAmount = created.TotalAmount - initialPaid,
                DueDate = request.DueDate?.UtcDateTime ?? DateTime.UtcNow.AddDays(30),
                PaymentStatus = (created.TotalAmount - initialPaid == 0) ? PaymentStatus.Paid : 
                                 (initialPaid > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid),
                Notes = created.Notes,
                CreatedBy = userId,
                Payments = new List<PayablePayment>()
            };

            if (initialPaid > 0)
            {
                var initialPayment = new PayablePayment
                {
                    PayableId = payable.Id,
                    PaymentDate = created.PurchaseDate,
                    Amount = initialPaid,
                    PaymentMethod = created.PaymentMethod,
                    Notes = "Pembayaran awal saat pembelian",
                    CreatedBy = userId
                };
                payable.Payments.Add(initialPayment);
            }

            await _payableRepository.AddAsync(payable, cancellationToken);
        }

        // Update stock and create stock movements
        var storeSettings = await _storeSettingsRepository.GetAsync(cancellationToken);
        var allowNegativeStock = storeSettings?.AllowNegativeStock ?? false;

        foreach (var item in items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

            // Update purchase price if tracking is enabled
            if (storeSettings?.EnablePurchasePriceTracking == true && product != null)
            {
                product.PurchasePrice = item.UnitPrice;
            }

            if (product != null)
            {
                var quantityBefore = product.CurrentStock;
                product.CurrentStock += item.Quantity;
                product.UpdatedBy = userId;

                // Create stock movement
                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    MovementDate = purchase.PurchaseDate,
                    MovementType = StockMovementType.Purchase,
                    QuantityBefore = quantityBefore,
                    QuantityChange = item.Quantity,
                    QuantityAfter = product.CurrentStock,
                    ReferenceType = "Purchase",
                    ReferenceId = purchase.Id,
                    Notes = $"Pembelian dari {supplier.Name}"
                };

                await _stockMovementRepository.AddAsync(movement, cancellationToken);
            }
        }

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "CreatePurchase",
            EntityName = "Purchase",
            EntityId = purchase.Id,
            Module = "Purchases"
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<PurchaseResponse?> VoidAsync(
        Guid id,
        string reason,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (purchase == null) return null;

        if (purchase.Status == TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Pembelian sudah pernah dibatalkan.");
        }

        var storeSettings = await _storeSettingsRepository.GetAsync(cancellationToken);
        var allowNegativeStock = storeSettings?.AllowNegativeStock ?? false;

        // Reverse stock movements
        var productIds = purchase.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var item in purchase.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

            var quantityBefore = product.CurrentStock;
            var newStock = product.CurrentStock - item.Quantity;

            if (!allowNegativeStock && newStock < 0)
            {
                throw new BusinessRuleException(
                    $"Pembatalan tidak dapat dilakukan. " +
                    $"Stok '{product.Name}' akan menjadi negatif ({newStock}).");
            }

            product.CurrentStock = newStock;
            product.UpdatedBy = userId;

            // Create reverse stock movement
            var movement = new StockMovement
            {
                ProductId = product.Id,
                MovementDate = DateTime.UtcNow,
                MovementType = StockMovementType.PurchaseReturn,
                QuantityBefore = quantityBefore,
                QuantityChange = -item.Quantity,
                QuantityAfter = newStock,
                ReferenceType = "PurchaseVoid",
                ReferenceId = purchase.Id,
                Notes = $"Pembatalan pembelian: {reason}"
            };

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
        }

        // Update purchase status
        purchase.Status = TransactionStatus.Voided;
        purchase.VoidReason = reason;
        purchase.VoidedBy = userId;
        purchase.VoidedAt = DateTime.UtcNow;
        purchase.UpdatedBy = userId;

        await _purchaseRepository.UpdateAsync(purchase, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "VoidPurchase",
            EntityName = "Purchase",
            EntityId = purchase.Id,
            Module = "Purchases"
        }, cancellationToken);

        return MapToResponse(purchase);
    }

    private async Task<string> GeneratePurchaseNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PUR-{today:yyyyMMdd}-";

        var lastNumber = await _purchaseRepository.GetByNumberAsync("");
        // Get last purchase number with same prefix
        var purchases = await _purchaseRepository.GetAllAsync(null, null, null, null, 1, 100, cancellationToken);
        var lastPurchase = purchases.FirstOrDefault(p => p.PurchaseNumber.StartsWith(prefix));

        if (lastPurchase == null)
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastPurchase.PurchaseNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }

    private async Task<string> GeneratePayableNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PAY-{today:yyyyMMdd}-";

        var lastPayableNumber = await _payableRepository.GetLastPayableNumberAsync(prefix, cancellationToken);

        if (string.IsNullOrEmpty(lastPayableNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastPayableNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }

    private static PurchaseResponse MapToResponse(Purchase purchase) => new()
    {
        Id = purchase.Id,
        PurchaseNumber = purchase.PurchaseNumber,
        PurchaseDate = purchase.PurchaseDate,
        Supplier = purchase.Supplier != null ? new SupplierResponse
        {
            Id = purchase.Supplier.Id,
            Name = purchase.Supplier.Name
        } : null,
        Items = purchase.Items.Select(i => new PurchaseItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? i.ProductSnapshotName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.Subtotal
        }).ToList(),
        Subtotal = purchase.Subtotal,
        DiscountAmount = purchase.DiscountAmount,
        TotalAmount = purchase.TotalAmount,
        PaymentMethod = purchase.PaymentMethod.ToString(),
        PaymentStatus = purchase.PaymentStatus.ToString(),
        Status = purchase.Status.ToString(),
        Notes = purchase.Notes
    };

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (purchase == null) return false;

        var payables = await _payableRepository.GetAllAsync(null, null, 1, 100, cancellationToken);
        var relatedPayable = payables.FirstOrDefault(p => p.PurchaseId == id);
        if (relatedPayable != null)
        {
            await _payableRepository.DeleteAsync(relatedPayable, cancellationToken);
        }

        await _purchaseRepository.DeleteAsync(purchase, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Delete",
            EntityName = "Purchase",
            EntityId = purchase.Id,
            Module = "Purchases",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
