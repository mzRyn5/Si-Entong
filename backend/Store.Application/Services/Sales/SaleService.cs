using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Sales;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Sales;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;

namespace Store.Application.Services.Sales;

public interface ISaleService
{
    Task<PagedResponse<SaleListItemResponse>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SaleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, Guid cashierId, CancellationToken cancellationToken = default);
    Task<SaleResponse?> VoidAsync(Guid id, string reason, Guid userId, CancellationToken cancellationToken = default);
    Task<SaleReceiptResponse?> GetReceiptAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStoreSettingsRepository _storeSettingsRepository;
    private readonly IStoreProfileRepository _storeProfileRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IReceivableRepository _receivableRepository;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ICashSessionRepository cashSessionRepository,
        IStockMovementRepository stockMovementRepository,
        IStoreSettingsRepository storeSettingsRepository,
        IStoreProfileRepository storeProfileRepository,
        IAuditLogRepository auditLogRepository,
        IReceivableRepository receivableRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _cashSessionRepository = cashSessionRepository;
        _stockMovementRepository = stockMovementRepository;
        _storeSettingsRepository = storeSettingsRepository;
        _storeProfileRepository = storeProfileRepository;
        _auditLogRepository = auditLogRepository;
        _receivableRepository = receivableRepository;
    }

    public async Task<PagedResponse<SaleListItemResponse>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.GetAllAsync(
            cashierId, fromDate, toDate, status, page, pageSize, cancellationToken);
        var totalCount = await _saleRepository.GetTotalCountAsync(
            cashierId, fromDate, toDate, status, cancellationToken);

        return new PagedResponse<SaleListItemResponse>
        {
            Data = sales.Select(s => new SaleListItemResponse
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                SaleDate = s.SaleDate,
                CashierName = s.Cashier?.Name ?? string.Empty,
                CustomerName = s.Customer?.Name,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod.ToString(),
                PaymentStatus = s.PaymentStatus.ToString(),
                Status = s.Status.ToString()
            }).ToList(),
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<SaleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        return sale == null ? null : MapToResponse(sale);
    }

    public async Task<SaleResponse> CreateAsync(
        CreateSaleRequest request,
        Guid cashierId,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new BusinessRuleException("Minimal 1 item penjualan.");
        }

        var storeSettings = await _storeSettingsRepository.GetAsync(cancellationToken);
        var allowNegativeStock = storeSettings?.AllowNegativeStock ?? false;
        var requireCashSession = false; // Disable cashier session requirement for simplified flow

        CashSession? cashSession = null;
        if (requireCashSession)
        {
            cashSession = request.CashSessionId.HasValue
                ? await _cashSessionRepository.GetByIdAsync(request.CashSessionId.Value, cancellationToken)
                : await _cashSessionRepository.GetActiveByCashierIdAsync(cashierId, cancellationToken);

            if (cashSession == null || cashSession.CashierId != cashierId || cashSession.Status != CashSessionStatus.Open)
            {
                throw new BusinessRuleException("Kasir wajib memiliki sesi kasir aktif.");
            }
        }
        else if (request.CashSessionId.HasValue)
        {
            cashSession = await _cashSessionRepository.GetByIdAsync(request.CashSessionId.Value, cancellationToken);
            if (cashSession == null || cashSession.Status != CashSessionStatus.Open)
            {
                throw new BusinessRuleException("Sesi kasir tidak aktif atau tidak ditemukan.");
            }
        }

        if (request.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
            if (customer == null || !customer.IsActive)
            {
                throw new BusinessRuleException("Pelanggan tidak aktif atau tidak ditemukan.");
            }
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var items = new List<SaleItem>();
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

            if (item.UnitPrice < 0 || item.DiscountAmount < 0)
            {
                throw new BusinessRuleException("Harga dan diskon item tidak boleh negatif.");
            }

            var itemSubtotal = item.Quantity * item.UnitPrice - item.DiscountAmount;
            if (itemSubtotal < 0)
            {
                throw new BusinessRuleException("Diskon item tidak boleh melebihi subtotal item.");
            }

            if (!allowNegativeStock && product.CurrentStock < item.Quantity)
            {
                throw new BusinessRuleException(
                    $"Stok '{product.Name}' tidak mencukupi. Stok saat ini {product.CurrentStock}, diminta {item.Quantity}.");
            }

            items.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                Subtotal = itemSubtotal,
                ProductSnapshotName = product.Name,
                ProductSnapshotSku = product.Sku,
                PurchasePriceSnapshot = product.PurchasePrice
            });

            subtotal += itemSubtotal;
        }

        if (request.DiscountAmount < 0 || request.TaxAmount < 0)
        {
            throw new BusinessRuleException("Diskon dan pajak tidak boleh negatif.");
        }

        var totalAmount = subtotal - request.DiscountAmount + request.TaxAmount;
        if (totalAmount < 0)
        {
            throw new BusinessRuleException("Diskon transaksi tidak boleh melebihi subtotal.");
        }

        var amountPaid = totalAmount;
        var changeAmount = 0m;
        var paymentStatus = request.PaymentStatus;

        if (paymentStatus == PaymentStatus.Paid)
        {
            amountPaid = request.PaymentMethod == PaymentMethod.Cash ? request.AmountPaid : totalAmount;
            changeAmount = request.PaymentMethod == PaymentMethod.Cash ? amountPaid - totalAmount : 0;
            if (request.PaymentMethod == PaymentMethod.Cash && amountPaid < totalAmount)
            {
                throw new BusinessRuleException("Uang diterima tidak boleh kurang dari total transaksi.");
            }
        }
        else if (paymentStatus == PaymentStatus.Unpaid)
        {
            amountPaid = 0;
            changeAmount = 0;
        }
        else if (paymentStatus == PaymentStatus.Partial)
        {
            amountPaid = request.AmountPaid;
            changeAmount = 0;
            if (amountPaid <= 0)
            {
                throw new BusinessRuleException("Nominal bayar sebagian harus lebih besar dari 0.");
            }
            if (amountPaid >= totalAmount)
            {
                throw new BusinessRuleException("Nominal bayar sebagian tidak boleh melebihi atau sama dengan total transaksi.");
            }
        }

        var sale = new Sale
        {
            SaleNumber = await _saleRepository.GenerateSaleNumberAsync(cancellationToken),
            SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
            CashierId = cashierId,
            CustomerId = request.CustomerId,
            CashSessionId = cashSession?.Id,
            Subtotal = subtotal,
            DiscountAmount = request.DiscountAmount,
            TaxAmount = request.TaxAmount,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            AmountPaid = amountPaid,
            ChangeAmount = changeAmount,
            PaymentStatus = paymentStatus,
            Status = TransactionStatus.Completed,
            Notes = request.Notes,
            CreatedBy = cashierId
        };

        foreach (var item in items)
        {
            sale.Items.Add(item);
        }

        var created = await _saleRepository.AddAsync(sale, cancellationToken);

        // Generate Receivable record if Unpaid or Partial
        if (created.PaymentStatus == PaymentStatus.Unpaid || created.PaymentStatus == PaymentStatus.Partial)
        {
            if (!created.CustomerId.HasValue)
            {
                throw new BusinessRuleException("Pelanggan (Customer) wajib diisi untuk transaksi kredit/piutang.");
            }

            var receivableNumber = await GenerateReceivableNumberAsync(cancellationToken);
            var receivable = new Receivable
            {
                ReceivableNumber = receivableNumber,
                SaleId = created.Id,
                CustomerId = created.CustomerId.Value,
                TotalAmount = created.TotalAmount,
                PaidAmount = created.AmountPaid,
                RemainingAmount = created.TotalAmount - created.AmountPaid,
                DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30),
                PaymentStatus = created.PaymentStatus,
                Notes = created.Notes,
                CreatedBy = cashierId
            };
            await _receivableRepository.AddAsync(receivable, cancellationToken);
        }

        foreach (var item in created.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

            var quantityBefore = product.CurrentStock;
            var quantityAfter = quantityBefore - item.Quantity;
            product.CurrentStock = quantityAfter;
            product.UpdatedBy = cashierId;
            await _productRepository.UpdateAsync(product, cancellationToken);

            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ProductId = product.Id,
                MovementDate = created.SaleDate,
                MovementType = StockMovementType.Sale,
                QuantityBefore = quantityBefore,
                QuantityChange = -item.Quantity,
                QuantityAfter = quantityAfter,
                ReferenceType = "Sale",
                ReferenceId = created.Id,
                Notes = $"Penjualan {created.SaleNumber}",
                CreatedBy = cashierId
            }, cancellationToken);
        }

        if (created.PaymentMethod == PaymentMethod.Cash && cashSession != null)
        {
            cashSession.CashSalesAmount += created.TotalAmount;
            cashSession.UpdatedBy = cashierId;
            await _cashSessionRepository.UpdateAsync(cashSession, cancellationToken);
        }

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = cashierId,
            Action = "CreateSale",
            EntityName = "Sale",
            EntityId = created.Id,
            Module = "Sales",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var result = await _saleRepository.GetByIdAsync(created.Id, cancellationToken);
        return MapToResponse(result ?? created);
    }

    public async Task<SaleResponse?> VoidAsync(
        Guid id,
        string reason,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        if (sale == null) return null;

        if (sale.Status == TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Penjualan sudah pernah dibatalkan.");
        }

        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        foreach (var item in sale.Items)
        {
            if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

            var quantityBefore = product.CurrentStock;
            var quantityAfter = quantityBefore + item.Quantity;
            product.CurrentStock = quantityAfter;
            product.UpdatedBy = userId;
            await _productRepository.UpdateAsync(product, cancellationToken);

            await _stockMovementRepository.AddAsync(new StockMovement
            {
                ProductId = product.Id,
                MovementDate = DateTime.UtcNow,
                MovementType = StockMovementType.SalesReturn,
                QuantityBefore = quantityBefore,
                QuantityChange = item.Quantity,
                QuantityAfter = quantityAfter,
                ReferenceType = "SaleVoid",
                ReferenceId = sale.Id,
                Notes = $"Pembatalan penjualan: {reason}",
                CreatedBy = userId
            }, cancellationToken);
        }

        if (sale.PaymentMethod == PaymentMethod.Cash && sale.CashSessionId.HasValue)
        {
            var cashSession = await _cashSessionRepository.GetByIdAsync(sale.CashSessionId.Value, cancellationToken);
            if (cashSession != null)
            {
                cashSession.CashSalesAmount -= sale.TotalAmount;
                cashSession.UpdatedBy = userId;
                await _cashSessionRepository.UpdateAsync(cashSession, cancellationToken);
            }
        }

        sale.Status = TransactionStatus.Voided;
        sale.VoidReason = reason;
        sale.VoidedBy = userId;
        sale.VoidedAt = DateTime.UtcNow;
        sale.UpdatedBy = userId;
        await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "VoidSale",
            EntityName = "Sale",
            EntityId = sale.Id,
            Module = "Sales",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(sale);
    }

    public async Task<SaleReceiptResponse?> GetReceiptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        if (sale == null) return null;

        var storeProfile = await _storeProfileRepository.GetAsync(cancellationToken);

        return new SaleReceiptResponse
        {
            StoreName = storeProfile?.Name ?? "Toko Kelontong",
            StoreAddress = storeProfile?.Address ?? string.Empty,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            CashierName = sale.Cashier?.Name ?? string.Empty,
            Items = sale.Items.Select(i => new SaleReceiptItemResponse
            {
                Name = i.ProductSnapshotName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList(),
            TotalAmount = sale.TotalAmount,
            AmountPaid = sale.AmountPaid,
            ChangeAmount = sale.ChangeAmount
        };
    }

    private async Task<string> GenerateReceivableNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"REC-{today:yyyyMMdd}-";

        var lastReceivableNumber = await _receivableRepository.GetLastReceivableNumberAsync(prefix, cancellationToken);

        if (string.IsNullOrEmpty(lastReceivableNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastReceivableNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }

    private static SaleResponse MapToResponse(Sale sale) => new()
    {
        Id = sale.Id,
        SaleNumber = sale.SaleNumber,
        SaleDate = sale.SaleDate,
        Cashier = sale.Cashier != null ? new CashierResponse
        {
            Id = sale.Cashier.Id,
            Name = sale.Cashier.Name
        } : null,
        Customer = sale.Customer != null ? new CustomerResponse
        {
            Id = sale.Customer.Id,
            Name = sale.Customer.Name
        } : null,
        Items = sale.Items.Select(i => new SaleItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? i.ProductSnapshotName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountAmount = i.DiscountAmount,
            Subtotal = i.Subtotal
        }).ToList(),
        Subtotal = sale.Subtotal,
        DiscountAmount = sale.DiscountAmount,
        TaxAmount = sale.TaxAmount,
        TotalAmount = sale.TotalAmount,
        PaymentMethod = sale.PaymentMethod.ToString(),
        AmountPaid = sale.AmountPaid,
        ChangeAmount = sale.ChangeAmount,
        PaymentStatus = sale.PaymentStatus.ToString(),
        Status = sale.Status.ToString(),
        Notes = sale.Notes
    };

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        if (sale == null) return false;

        var receivables = await _receivableRepository.GetAllAsync(null, null, 1, 100, cancellationToken);
        var relatedReceivable = receivables.FirstOrDefault(r => r.SaleId == id);
        if (relatedReceivable != null)
        {
            await _receivableRepository.DeleteAsync(relatedReceivable, cancellationToken);
        }

        await _saleRepository.DeleteAsync(sale, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Delete",
            EntityName = "Sale",
            EntityId = sale.Id,
            Module = "Sales",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
