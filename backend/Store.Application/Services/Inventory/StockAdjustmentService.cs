using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Inventory;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using Store.SharedKernel;

namespace Store.Application.Services.Inventory;

public interface IStockAdjustmentService
{
    Task<PagedResponse<StockAdjustmentListItemResponse>> GetAllAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CreateStockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResponse?> UpdateAsync(
        Guid id,
        UpdateStockAdjustmentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResponse?> PostAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResponse?> VoidAsync(
        Guid id,
        string reason,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly IStockAdjustmentRepository _stockAdjustmentRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStoreSettingsRepository _storeSettingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public StockAdjustmentService(
        IStockAdjustmentRepository stockAdjustmentRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IStoreSettingsRepository storeSettingsRepository,
        IAuditLogRepository auditLogRepository)
    {
        _stockAdjustmentRepository = stockAdjustmentRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _storeSettingsRepository = storeSettingsRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<StockAdjustmentListItemResponse>> GetAllAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var adjustments = await _stockAdjustmentRepository.GetAllAsync(
            fromDate, toDate, status, page, pageSize, cancellationToken);

        var totalCount = await _stockAdjustmentRepository.GetTotalCountAsync(
            fromDate, toDate, status, cancellationToken);

        var responses = adjustments.Select(a => new StockAdjustmentListItemResponse
        {
            Id = a.Id,
            AdjustmentNumber = a.AdjustmentNumber,
            AdjustmentDate = a.AdjustmentDate,
            Reason = a.Reason,
            Status = a.Status.ToString(),
            TotalItems = a.Items.Count
        }).ToList();

        return new PagedResponse<StockAdjustmentListItemResponse>
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

    public async Task<StockAdjustmentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await _stockAdjustmentRepository.GetByIdAsync(id, cancellationToken);
        if (adjustment == null) return null;

        return MapToResponse(adjustment);
    }

    public async Task<CreateStockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleException("Alasan koreksi stok wajib diisi.");
        }

        if (request.Items.Count == 0)
        {
            throw new BusinessRuleException("Minimal 1 item koreksi stok.");
        }

        // Generate adjustment number
        var adjustmentNumber = await _stockAdjustmentRepository.GenerateAdjustmentNumberAsync(cancellationToken);

        // Create adjustment
        var adjustment = new StockAdjustment
        {
            AdjustmentNumber = adjustmentNumber,
            AdjustmentDate = request.AdjustmentDate.UtcDateTime,
            Reason = request.Reason,
            Notes = request.Notes,
            Status = TransactionStatus.Draft,
            CreatedBy = userId
        };

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

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

            if (!Enum.TryParse<StockAdjustmentType>(item.AdjustmentType, true, out var adjustmentType))
            {
                throw new BusinessRuleException($"Tipe koreksi '{item.AdjustmentType}' tidak valid. Gunakan 'Increase' atau 'Decrease'.");
            }

            adjustment.Items.Add(new StockAdjustmentItem
            {
                ProductId = item.ProductId,
                AdjustmentType = adjustmentType,
                Quantity = item.Quantity,
                Notes = item.Notes
            });
        }

        var created = await _stockAdjustmentRepository.AddAsync(adjustment, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "CreateStockAdjustment",
            EntityName = "StockAdjustment",
            EntityId = created.Id,
            Module = "Inventory"
        }, cancellationToken);

        return new CreateStockAdjustmentResponse
        {
            Id = created.Id,
            AdjustmentNumber = created.AdjustmentNumber,
            Status = created.Status.ToString()
        };
    }

    public async Task<StockAdjustmentResponse?> UpdateAsync(
        Guid id,
        UpdateStockAdjustmentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await _stockAdjustmentRepository.GetByIdAsync(id, cancellationToken);
        if (adjustment == null) return null;

        if (adjustment.Status != TransactionStatus.Draft)
        {
            throw new BusinessRuleException("Hanya koreksi stok berstatus Draft yang dapat diubah.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleException("Alasan koreksi stok wajib diisi.");
        }

        if (request.Items.Count == 0)
        {
            throw new BusinessRuleException("Minimal 1 item koreksi stok.");
        }

        // Update basic info
        adjustment.AdjustmentDate = request.AdjustmentDate.UtcDateTime;
        adjustment.Reason = request.Reason;
        adjustment.Notes = request.Notes;
        adjustment.UpdatedBy = userId;

        // Clear existing items and add new ones
        adjustment.Items.Clear();

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

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

            if (!Enum.TryParse<StockAdjustmentType>(item.AdjustmentType, true, out var adjustmentType))
            {
                throw new BusinessRuleException($"Tipe koreksi '{item.AdjustmentType}' tidak valid.");
            }

            adjustment.Items.Add(new StockAdjustmentItem
            {
                StockAdjustmentId = adjustment.Id,
                ProductId = item.ProductId,
                AdjustmentType = adjustmentType,
                Quantity = item.Quantity,
                Notes = item.Notes
            });
        }

        await _stockAdjustmentRepository.UpdateAsync(adjustment, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "UpdateStockAdjustment",
            EntityName = "StockAdjustment",
            EntityId = adjustment.Id,
            Module = "Inventory"
        }, cancellationToken);

        return MapToResponse(adjustment);
    }

    public async Task<StockAdjustmentResponse?> PostAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await _stockAdjustmentRepository.GetByIdAsync(id, cancellationToken);
        if (adjustment == null) return null;

        if (adjustment.Status != TransactionStatus.Draft)
        {
            throw new BusinessRuleException("Hanya koreksi stok berstatus Draft yang dapat diposting.");
        }

        var storeSettings = await _storeSettingsRepository.GetAsync(cancellationToken);
        var allowNegativeStock = storeSettings?.AllowNegativeStock ?? false;

        // Validate and process each item
        var productIds = adjustment.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var item in adjustment.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product))
            {
                throw new NotFoundException("Produk", item.ProductId.ToString());
            }

            var quantityChange = item.AdjustmentType == StockAdjustmentType.Increase
                ? item.Quantity
                : -item.Quantity;

            var newStock = product.CurrentStock + quantityChange;

            if (!allowNegativeStock && newStock < 0)
            {
                throw new BusinessRuleException(
                    $"Koreksi keluar untuk '{product.Name}' tidak dapat dilakukan. " +
                    $"Stok saat ini {product.CurrentStock}, diminta mengurangi {item.Quantity}.");
            }

            // Create stock movement
            var movement = new StockMovement
            {
                ProductId = product.Id,
                MovementDate = adjustment.AdjustmentDate,
                MovementType = item.AdjustmentType == StockAdjustmentType.Increase
                    ? StockMovementType.AdjustmentIn
                    : StockMovementType.AdjustmentOut,
                QuantityBefore = product.CurrentStock,
                QuantityChange = quantityChange,
                QuantityAfter = newStock,
                ReferenceType = "StockAdjustment",
                ReferenceId = adjustment.Id,
                Notes = $"Koreksi stok: {adjustment.Reason}"
            };

            await _stockMovementRepository.AddAsync(movement, cancellationToken);

            // Update product stock
            product.CurrentStock = newStock;
            product.UpdatedBy = userId;
            await Task.CompletedTask; // Product update would be done in transaction
        }

        // Update adjustment status
        adjustment.Status = TransactionStatus.Posted;
        adjustment.UpdatedBy = userId;
        await _stockAdjustmentRepository.UpdateAsync(adjustment, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "PostStockAdjustment",
            EntityName = "StockAdjustment",
            EntityId = adjustment.Id,
            Module = "Inventory"
        }, cancellationToken);

        return MapToResponse(adjustment);
    }

    public async Task<StockAdjustmentResponse?> VoidAsync(
        Guid id,
        string reason,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var adjustment = await _stockAdjustmentRepository.GetByIdAsync(id, cancellationToken);
        if (adjustment == null) return null;

        if (adjustment.Status == TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Koreksi stok sudah pernah dibatalkan.");
        }

        if (adjustment.Status == TransactionStatus.Draft)
        {
            // Just mark as voided without reversing stock movements
            adjustment.Status = TransactionStatus.Voided;
            adjustment.VoidReason = reason;
            adjustment.VoidedBy = userId;
            adjustment.VoidedAt = DateTime.UtcNow;
            adjustment.UpdatedBy = userId;
        }
        else
        {
            // Posted adjustment - need to reverse stock movements
            var storeSettings = await _storeSettingsRepository.GetAsync(cancellationToken);
            var allowNegativeStock = storeSettings?.AllowNegativeStock ?? false;

            var productIds = adjustment.Items.Select(i => i.ProductId).Distinct().ToList();
            var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var item in adjustment.Items)
            {
                if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

                var quantityChange = item.AdjustmentType == StockAdjustmentType.Increase
                    ? -item.Quantity
                    : item.Quantity;

                var newStock = product.CurrentStock + quantityChange;

                if (!allowNegativeStock && newStock < 0)
                {
                    throw new BusinessRuleException(
                        $"Pembatalan tidak dapat dilakukan. " +
                        $"Stok '{product.Name}' akan menjadi negatif ({newStock}).");
                }

                // Create reverse stock movement
                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    MovementDate = DateTime.UtcNow,
                    MovementType = item.AdjustmentType == StockAdjustmentType.Increase
                        ? StockMovementType.AdjustmentOut
                        : StockMovementType.AdjustmentIn,
                    QuantityBefore = product.CurrentStock,
                    QuantityChange = quantityChange,
                    QuantityAfter = newStock,
                    ReferenceType = "StockAdjustmentVoid",
                    ReferenceId = adjustment.Id,
                    Notes = $"Pembatalan koreksi stok: {reason}"
                };

                await _stockMovementRepository.AddAsync(movement, cancellationToken);

                product.CurrentStock = newStock;
                product.UpdatedBy = userId;
            }

            adjustment.Status = TransactionStatus.Voided;
            adjustment.VoidReason = reason;
            adjustment.VoidedBy = userId;
            adjustment.VoidedAt = DateTime.UtcNow;
            adjustment.UpdatedBy = userId;
        }

        await _stockAdjustmentRepository.UpdateAsync(adjustment, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "VoidStockAdjustment",
            EntityName = "StockAdjustment",
            EntityId = adjustment.Id,
            Module = "Inventory"
        }, cancellationToken);

        return MapToResponse(adjustment);
    }

    private static StockAdjustmentResponse MapToResponse(StockAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        AdjustmentNumber = adjustment.AdjustmentNumber,
        AdjustmentDate = adjustment.AdjustmentDate,
        Reason = adjustment.Reason,
        Notes = adjustment.Notes,
        Status = adjustment.Status.ToString(),
        CreatedAt = adjustment.CreatedAt,
        Items = adjustment.Items.Select(i => new StockAdjustmentItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            AdjustmentType = i.AdjustmentType.ToString(),
            Quantity = i.Quantity,
            Notes = i.Notes
        }).ToList()
    };
}
