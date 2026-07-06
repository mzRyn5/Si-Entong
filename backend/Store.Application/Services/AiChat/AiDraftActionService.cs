using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.AuditLogs;
using Store.Application.Services.Inventory;
using Store.Application.Services.MasterData;
using Store.Application.Services.Purchases;
using Store.Application.Services.Sales;
using Store.Application.Services.Expenses;
using Store.Application.Services.Returns;
using Store.Application.Services.Payables;
using Store.Application.Services.Receivables;
using Store.Contracts.AiChat;
using Store.Contracts.Requests.Sales;
using Store.Contracts.Requests.Purchases;
using Store.Contracts.Requests.Receivables;
using Store.Contracts.Requests.Payables;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Requests.Products;
using Store.Contracts.Requests.Customers;
using Store.Contracts.Requests.Suppliers;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;

namespace Store.Application.Services.AiChat;

public sealed class AiDraftActionService : IAiDraftActionService
{
    private readonly IAiChatRepository _chatRepo;
    private readonly ISaleService _saleService;
    private readonly IPurchaseService _purchaseService;
    private readonly IReceivableRepository _receivableRepository;
    private readonly IReceivableService _receivableService;
    private readonly IPayableRepository _payableRepository;
    private readonly IPayableService _payableService;
    private readonly IStockAdjustmentService _stockAdjustmentService;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;

    public AiDraftActionService(
        IAiChatRepository chatRepo,
        ISaleService saleService,
        IPurchaseService purchaseService,
        IReceivableRepository receivableRepository,
        IReceivableService receivableService,
        IPayableRepository payableRepository,
        IPayableService payableService,
        IStockAdjustmentService stockAdjustmentService,
        IProductRepository productRepository,
        IAuditLogRepository auditLogRepository,
        IProductService productService,
        ICustomerService customerService,
        ISupplierService supplierService)
    {
        _chatRepo = chatRepo;
        _saleService = saleService;
        _purchaseService = purchaseService;
        _receivableRepository = receivableRepository;
        _receivableService = receivableService;
        _payableRepository = payableRepository;
        _payableService = payableService;
        _stockAdjustmentService = stockAdjustmentService;
        _productRepository = productRepository;
        _auditLogRepository = auditLogRepository;
        _productService = productService;
        _customerService = customerService;
        _supplierService = supplierService;
    }

    public async Task<AiActionResponse> ExecuteAsync(AiActionRequest request, Guid userId, Guid storeId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            return new AiActionResponse { Success = false, Message = "Session ID tidak valid." };
        }

        string? draftIdObj = null;
        if (request.Payload != null)
        {
            if (request.Payload is JsonElement jsonEl)
            {
                if (jsonEl.TryGetProperty("draftId", out var idProp))
                {
                    draftIdObj = idProp.GetString();
                }
            }
            else
            {
                var prop = request.Payload.GetType().GetProperty("draftId");
                if (prop != null)
                {
                    draftIdObj = prop.GetValue(request.Payload)?.ToString();
                }
                else
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(request.Payload);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("draftId", out var idProp))
                        {
                            draftIdObj = idProp.GetString();
                        }
                    }
                    catch { }
                }
            }
        }

        if (string.IsNullOrEmpty(draftIdObj) || !Guid.TryParse(draftIdObj, out var draftId))
        {
            return new AiActionResponse { Success = false, Message = "Draft ID tidak valid atau tidak disediakan." };
        }

        var draft = await _chatRepo.GetDraftForUserAsync(draftId, sessionId, userId, storeId, cancellationToken);
        if (draft == null)
        {
            return new AiActionResponse { Success = false, Message = "Draft tidak ditemukan." };
        }

        if (draft.Status != "pending")
        {
            return new AiActionResponse { Success = false, Message = $"Draft sudah memiliki status '{draft.Status}'." };
        }

        if (draft.ExpiredAt < DateTime.UtcNow)
        {
            draft.Status = "expired";
            await _chatRepo.UpdateDraftStatusAsync(draft.Id, "expired", cancellationToken);
            return new AiActionResponse { Success = false, Message = "Draft transaksi sudah kedaluwarsa (berumur lebih dari 10 menit)." };
        }

        if (request.Action != null && request.Action.Equals("cancel_draft", StringComparison.OrdinalIgnoreCase))
        {
            await _chatRepo.UpdateDraftStatusAsync(draft.Id, "cancelled", cancellationToken);
            return new AiActionResponse
            {
                Success = true,
                Message = "Draft transaksi telah dibatalkan."
            };
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Guid? transactionId = null;
            string message = "Aksi berhasil dieksekusi.";

            switch (draft.ActionName)
            {
                case "create_sale":
                    {
                        var payload = JsonSerializer.Deserialize<SaleDraftPayload>(draft.DraftPayload, options);
                        if (payload == null || payload.Items.Count == 0)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft penjualan kosong." };
                        }

                        var saleRequest = new CreateSaleRequest
                        {
                            SaleDate = DateTime.UtcNow,
                            PaymentMethod = PaymentMethod.Cash,
                            PaymentStatus = PaymentStatus.Paid,
                            AmountPaid = payload.PaidAmount,
                            Items = payload.Items.Select(i => new CreateSaleItemRequest
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity,
                                UnitPrice = i.UnitPrice
                            }).ToList()
                        };

                        var saleResponse = await _saleService.CreateAsync(saleRequest, userId, cancellationToken);
                        transactionId = saleResponse.Id;
                        message = $"Penjualan {saleResponse.SaleNumber} berhasil disimpan.";
                    }
                    break;

                case "create_purchase":
                    {
                        var payload = JsonSerializer.Deserialize<PurchaseDraftPayload>(draft.DraftPayload, options);
                        if (payload == null || payload.Items.Count == 0)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft pembelian kosong." };
                        }

                        var purchaseRequest = new CreatePurchaseRequest
                        {
                            PurchaseDate = DateTimeOffset.UtcNow,
                            SupplierId = payload.SupplierId,
                            PaymentMethod = "Cash",
                            PaymentStatus = "Paid",
                            AmountPaid = payload.TotalAmount,
                            Items = payload.Items.Select(i => new CreatePurchaseItemRequest
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity,
                                UnitPrice = i.UnitCost
                            }).ToList()
                        };

                        var purchaseResponse = await _purchaseService.CreateAsync(purchaseRequest, userId, cancellationToken);
                        transactionId = purchaseResponse.Id;
                        message = $"Pembelian {purchaseResponse.PurchaseNumber} berhasil disimpan.";
                    }
                    break;

                case "create_receivable_payment":
                    {
                        var payload = JsonSerializer.Deserialize<PaymentDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft pembayaran piutang kosong." };
                        }

                        var amountRemaining = payload.Amount;

                        var receivables = await _receivableRepository.GetAllAsync(payload.CustomerId, "unpaid", 1, 100, cancellationToken);
                        var receivablesList = receivables.ToList();
                        var partialReceivables = await _receivableRepository.GetAllAsync(payload.CustomerId, "partial", 1, 100, cancellationToken);
                        receivablesList.AddRange(partialReceivables);

                        var sortedReceivables = receivablesList
                            .Where(r => r.RemainingAmount > 0)
                            .OrderBy(r => r.CreatedAt)
                            .ToList();

                        if (sortedReceivables.Count == 0)
                        {
                            return new AiActionResponse { Success = false, Message = "Tidak ditemukan piutang outstanding untuk pelanggan ini." };
                        }

                        foreach (var rec in sortedReceivables)
                        {
                            if (amountRemaining <= 0) break;

                            decimal payAmount = Math.Min(amountRemaining, rec.RemainingAmount);
                            var payRequest = new RecordReceivablePaymentRequest
                            {
                                PaymentDate = DateTime.UtcNow,
                                Amount = payAmount,
                                PaymentMethod = "Cash",
                                Notes = "Pembayaran otomatis via AI Assistant"
                            };

                            var res = await _receivableService.RecordPaymentAsync(rec.Id, payRequest, userId, cancellationToken);
                            amountRemaining -= payAmount;
                            transactionId = res.Id; // Keep last payment record ID
                        }

                        message = $"Pembayaran piutang pelanggan berhasil dicatat sebesar Rp{payload.Amount:N0}.";
                    }
                    break;

                case "create_payable_payment":
                    {
                        var payload = JsonSerializer.Deserialize<PaymentDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft pembayaran hutang kosong." };
                        }

                        var amountRemaining = payload.Amount;

                        var payables = await _payableRepository.GetAllAsync(payload.SupplierId, "unpaid", 1, 100, cancellationToken);
                        var payablesList = payables.ToList();
                        var partialPayables = await _payableRepository.GetAllAsync(payload.SupplierId, "partial", 1, 100, cancellationToken);
                        payablesList.AddRange(partialPayables);

                        var sortedPayables = payablesList
                            .Where(p => p.RemainingAmount > 0)
                            .OrderBy(p => p.CreatedAt)
                            .ToList();

                        if (sortedPayables.Count == 0)
                        {
                            return new AiActionResponse { Success = false, Message = "Tidak ditemukan hutang outstanding untuk supplier ini." };
                        }

                        foreach (var pay in sortedPayables)
                        {
                            if (amountRemaining <= 0) break;

                            decimal payAmount = Math.Min(amountRemaining, pay.RemainingAmount);
                            var payRequest = new RecordPayablePaymentRequest
                            {
                                PaymentDate = DateTime.UtcNow,
                                Amount = payAmount,
                                PaymentMethod = "Cash",
                                Notes = "Pembayaran otomatis via AI Assistant"
                            };

                            var res = await _payableService.RecordPaymentAsync(pay.Id, payRequest, userId, cancellationToken);
                            amountRemaining -= payAmount;
                            transactionId = res.Id; // Keep last payment record ID
                        }

                        message = $"Pembayaran hutang ke supplier berhasil dicatat sebesar Rp{payload.Amount:N0}.";
                    }
                    break;

                case "create_stock_adjustment":
                    {
                        var payload = JsonSerializer.Deserialize<StockAdjustmentDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft koreksi stok kosong." };
                        }

                        string type = payload.Difference > 0 ? "Increase" : "Decrease";
                        int qty = Math.Abs(payload.Difference);

                        var adjustmentRequest = new CreateStockAdjustmentRequest
                        {
                            AdjustmentDate = DateTimeOffset.UtcNow,
                            Reason = payload.Reason,
                            Notes = "Koreksi otomatis via AI Assistant",
                            Items = new List<CreateStockAdjustmentItemRequest>
                            {
                                new CreateStockAdjustmentItemRequest
                                {
                                    ProductId = payload.ProductId,
                                    AdjustmentType = type,
                                    Quantity = qty,
                                    Notes = payload.Reason
                                }
                            }
                        };

                        var response = await _stockAdjustmentService.CreateAsync(adjustmentRequest, userId, cancellationToken);
                        var posted = await _stockAdjustmentService.PostAsync(response.Id, userId, cancellationToken);
                        transactionId = response.Id;
                        message = $"Koreksi stok {response.AdjustmentNumber} berhasil disimpan dan diposting.";
                    }
                    break;

                case "update_product":
                    {
                        var payload = JsonSerializer.Deserialize<ProductUpdateDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft update produk kosong." };
                        }

                        var product = await _productRepository.GetByIdAsync(payload.ProductId, cancellationToken);
                        if (product == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Produk tidak ditemukan." };
                        }

                        if (payload.NewSellingPrice.HasValue)
                        {
                            product.SellingPrice = payload.NewSellingPrice.Value;
                            product.UpdatedBy = userId;
                            await _productRepository.UpdateAsync(product, cancellationToken);

                            // Audit Log
                            await _auditLogRepository.AddAsync(new AuditLog
                            {
                                UserId = userId,
                                Action = "UpdateProductPrice",
                                EntityName = "Product",
                                EntityId = product.Id,
                                Module = "Products",
                                CreatedAt = DateTime.UtcNow
                            }, cancellationToken);

                            transactionId = product.Id;
                            message = $"Harga jual {product.Name} berhasil diubah menjadi Rp{payload.NewSellingPrice.Value:N0}.";
                        }
                        else
                        {
                            return new AiActionResponse { Success = false, Message = "Tidak ada perubahan harga terdeteksi pada draft." };
                        }
                    }
                    break;

                case "create_product":
                    {
                        var payload = JsonSerializer.Deserialize<ProductDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft produk kosong." };
                        }

                        var productRequest = new CreateProductRequest
                        {
                            Name = payload.Name,
                            Sku = payload.Sku,
                            Barcode = payload.Barcode,
                            CategoryId = payload.CategoryId,
                            UnitId = payload.UnitId,
                            PurchasePrice = payload.PurchasePrice,
                            SellingPrice = payload.SellingPrice,
                            InitialStock = payload.InitialStock,
                            LowStockThreshold = 5,
                            IsActive = true
                        };

                        var responseProduct = await _productService.CreateAsync(productRequest, userId, cancellationToken);
                        transactionId = responseProduct.Id;
                        message = $"Produk master '{responseProduct.Name}' (SKU: {responseProduct.Sku}) berhasil dibuat.";
                    }
                    break;

                case "create_customer":
                    {
                        var payload = JsonSerializer.Deserialize<CustomerDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft pelanggan kosong." };
                        }

                        var customerRequest = new CreateCustomerRequest
                        {
                            Name = payload.Name,
                            Phone = payload.Phone,
                            Address = payload.Address,
                            IsActive = true
                        };

                        var responseCustomer = await _customerService.CreateAsync(customerRequest, userId, cancellationToken);
                        transactionId = responseCustomer.Id;
                        message = $"Pelanggan master '{responseCustomer.Name}' berhasil ditambahkan.";
                    }
                    break;

                case "create_supplier":
                    {
                        var payload = JsonSerializer.Deserialize<SupplierDraftPayload>(draft.DraftPayload, options);
                        if (payload == null)
                        {
                            return new AiActionResponse { Success = false, Message = "Isi draft supplier kosong." };
                        }

                        var supplierRequest = new CreateSupplierRequest
                        {
                            Name = payload.Name,
                            Phone = payload.Phone,
                            Address = payload.Address,
                            IsActive = true
                        };

                        var responseSupplier = await _supplierService.CreateAsync(supplierRequest, userId, cancellationToken);
                        transactionId = responseSupplier.Id;
                        message = $"Supplier master '{responseSupplier.Name}' berhasil ditambahkan.";
                    }
                    break;

                default:
                    return new AiActionResponse { Success = false, Message = $"Aksi '{draft.ActionName}' tidak didukung." };
            }

            // Commit draft
            await _chatRepo.UpdateDraftStatusAsync(draft.Id, "committed", cancellationToken);

            // Save AI Action Audit Log
            var log = new AiActionLog
            {
                SessionId = sessionId,
                UserId = userId,
                StoreId = draft.Session?.StoreId ?? Guid.Empty,
                ActionName = draft.ActionName + "_confirm",
                RequestPayload = draft.DraftPayload,
                ResponsePayload = JsonSerializer.Serialize(new { success = true, transactionId = transactionId, message = message }),
                Status = "success"
            };
            await _chatRepo.SaveActionLogAsync(log, cancellationToken);

            return new AiActionResponse
            {
                Success = true,
                Message = message,
                TransactionId = transactionId
            };
        }
        catch (Exception ex)
        {
            var log = new AiActionLog
            {
                SessionId = sessionId,
                UserId = userId,
                StoreId = draft.Session?.StoreId ?? Guid.Empty,
                ActionName = draft.ActionName + "_fail",
                RequestPayload = draft.DraftPayload,
                ResponsePayload = JsonSerializer.Serialize(new { success = false, error = ex.Message }),
                Status = "error"
            };
            await _chatRepo.SaveActionLogAsync(log, cancellationToken);

            return new AiActionResponse
            {
                Success = false,
                Message = "Gagal memproses draft AI. Silakan periksa data dan coba lagi dari menu manual."
            };
        }
    }
}

