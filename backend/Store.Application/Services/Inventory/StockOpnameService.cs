using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Inventory;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.Inventory;

public interface IStockOpnameService
{
    Task<PagedResponse<StockOpnameResponse>> GetAllAsync(DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StockOpnameResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockOpnameResponse> CreateAsync(CreateStockOpnameRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<StockOpnameResponse?> UpdateAsync(Guid id, UpdateStockOpnameRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<StockOpnameResponse?> PostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<StockOpnameResponse?> VoidAsync(Guid id, string reason, Guid userId, CancellationToken cancellationToken = default);
}

public class StockOpnameService : IStockOpnameService
{
    private readonly IStockOpnameRepository _opnameRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public StockOpnameService(
        IStockOpnameRepository opnameRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository)
    {
        _opnameRepository = opnameRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<StockOpnameResponse>> GetAllAsync(
        DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var opnames = await _opnameRepository.GetAllAsync(fromDate, toDate, status, page, pageSize, cancellationToken);
        var totalCount = await _opnameRepository.GetTotalCountAsync(fromDate, toDate, status, cancellationToken);

        var responses = opnames.Select(MapToResponse).ToList();

        return new PagedResponse<StockOpnameResponse>
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

    public async Task<StockOpnameResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var opname = await _opnameRepository.GetByIdAsync(id, cancellationToken);
        return opname == null ? null : MapToResponse(opname);
    }

    public async Task<StockOpnameResponse> CreateAsync(
        CreateStockOpnameRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("Stock opname harus berisi minimal satu produk.", "EMPTY_ITEMS");
        }

        var opnameNumber = await _opnameRepository.GenerateOpnameNumberAsync(cancellationToken);
        var opname = new StockOpname
        {
            OpnameNumber = opnameNumber,
            Notes = request.Notes,
            Status = "Draft",
            OpnameDate = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var itemReq in request.Items)
        {
            if (!productsDict.TryGetValue(itemReq.ProductId, out var product))
            {
                throw new BusinessRuleException($"Produk dengan ID '{itemReq.ProductId}' tidak ditemukan.", "PRODUCT_NOT_FOUND");
            }

            var item = new StockOpnameItem
            {
                ProductId = itemReq.ProductId,
                SystemStock = product.CurrentStock,
                ActualStock = itemReq.ActualStock,
                Difference = itemReq.ActualStock - product.CurrentStock,
                Notes = itemReq.Notes,
                Product = product
            };

            opname.Items.Add(item);
        }

        var created = await _opnameRepository.AddAsync(opname, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "StockOpname",
            EntityId = created.Id,
            Module = "Inventory",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<StockOpnameResponse?> UpdateAsync(
        Guid id, UpdateStockOpnameRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var opname = await _opnameRepository.GetByIdAsync(id, cancellationToken);
        if (opname == null) return null;

        if (opname.Status != "Draft")
        {
            throw new BusinessRuleException("Hanya draf opname stok yang dapat diperbarui.", "INVALID_STATUS");
        }

        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("Stock opname harus berisi minimal satu produk.", "EMPTY_ITEMS");
        }

        opname.Notes = request.Notes;
        opname.Items.Clear();

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var itemReq in request.Items)
        {
            if (!productsDict.TryGetValue(itemReq.ProductId, out var product))
            {
                throw new BusinessRuleException($"Produk dengan ID '{itemReq.ProductId}' tidak ditemukan.", "PRODUCT_NOT_FOUND");
            }

            var item = new StockOpnameItem
            {
                ProductId = itemReq.ProductId,
                SystemStock = product.CurrentStock,
                ActualStock = itemReq.ActualStock,
                Difference = itemReq.ActualStock - product.CurrentStock,
                Notes = itemReq.Notes,
                Product = product
            };

            opname.Items.Add(item);
        }

        opname.UpdatedBy = updatedBy;
        opname.UpdatedAt = DateTime.UtcNow;

        await _opnameRepository.UpdateAsync(opname, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "StockOpname",
            EntityId = opname.Id,
            Module = "Inventory",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(opname);
    }

    public async Task<StockOpnameResponse?> PostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var opname = await _opnameRepository.GetByIdAsync(id, cancellationToken);
        if (opname == null) return null;

        if (opname.Status != "Draft")
        {
            throw new BusinessRuleException("Hanya draf opname stok yang dapat diposting.", "INVALID_STATUS");
        }

        var productIds = opname.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var item in opname.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

            // Recalculate system stock based on current stock at the moment of posting
            int oldStock = product.CurrentStock;
            item.SystemStock = oldStock;
            item.Difference = item.ActualStock - oldStock;

            product.CurrentStock = item.ActualStock;
            await _productRepository.UpdateAsync(product, cancellationToken);

            if (item.Difference != 0)
            {
                var movement = new StockMovement
                {
                    ProductId = item.ProductId,
                    MovementDate = DateTime.UtcNow,
                    MovementType = Domain.Enums.StockMovementType.StockOpname,
                    QuantityBefore = oldStock,
                    QuantityChange = item.Difference,
                    QuantityAfter = item.ActualStock,
                    ReferenceType = "StockOpname",
                    ReferenceId = opname.Id,
                    Notes = $"Stock Opname: {opname.OpnameNumber}",
                    CreatedBy = userId
                };

                await _stockMovementRepository.AddAsync(movement, cancellationToken);
            }
        }

        opname.Status = "Posted";
        opname.UpdatedBy = userId;
        opname.UpdatedAt = DateTime.UtcNow;

        await _opnameRepository.UpdateAsync(opname, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Post",
            EntityName = "StockOpname",
            EntityId = opname.Id,
            Module = "Inventory",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(opname);
    }

    public async Task<StockOpnameResponse?> VoidAsync(Guid id, string reason, Guid userId, CancellationToken cancellationToken = default)
    {
        var opname = await _opnameRepository.GetByIdAsync(id, cancellationToken);
        if (opname == null) return null;

        if (opname.Status == "Voided")
        {
            throw new BusinessRuleException("Stock opname sudah dibatalkan (voided).", "INVALID_STATUS");
        }

        if (opname.Status == "Draft")
        {
            opname.Status = "Voided";
            opname.UpdatedBy = userId;
            opname.UpdatedAt = DateTime.UtcNow;

            await _opnameRepository.UpdateAsync(opname, cancellationToken);
        }
        else if (opname.Status == "Posted")
        {
            // Reverse stock movements
            var productIds = opname.Items.Select(i => i.ProductId).Distinct().ToList();
            var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var item in opname.Items)
            {
                if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

                int oldStock = product.CurrentStock;
                int reverseChange = -item.Difference;
                int newStock = oldStock + reverseChange;

                product.CurrentStock = newStock;
                await _productRepository.UpdateAsync(product, cancellationToken);

                if (reverseChange != 0)
                {
                    var movement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        MovementDate = DateTime.UtcNow,
                        MovementType = Domain.Enums.StockMovementType.StockOpname,
                        QuantityBefore = oldStock,
                        QuantityChange = reverseChange,
                        QuantityAfter = newStock,
                        ReferenceType = "StockOpnameVoid",
                        ReferenceId = opname.Id,
                        Notes = $"Void Stock Opname: {opname.OpnameNumber}",
                        CreatedBy = userId
                    };

                    await _stockMovementRepository.AddAsync(movement, cancellationToken);
                }
            }

            opname.Status = "Voided";
            opname.UpdatedBy = userId;
            opname.UpdatedAt = DateTime.UtcNow;

            await _opnameRepository.UpdateAsync(opname, cancellationToken);
        }

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Void",
            EntityName = "StockOpname",
            EntityId = opname.Id,
            Module = "Inventory",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(opname);
    }

    private StockOpnameResponse MapToResponse(StockOpname opname)
    {
        return new StockOpnameResponse
        {
            Id = opname.Id,
            OpnameNumber = opname.OpnameNumber,
            Status = opname.Status,
            OpnameDate = opname.OpnameDate,
            Items = opname.Items.Select(i => new StockOpnameItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                SystemStock = i.SystemStock,
                ActualStock = i.ActualStock,
                Difference = i.Difference
            }).ToList()
        };
    }
}
