using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.AiChat.Tools;

public sealed class SalesPurchaseDraftToolHandler : IAiToolHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IAiChatRepository _aiChatRepository;

    public SalesPurchaseDraftToolHandler(
        IProductRepository productRepository,
        ISupplierRepository supplierRepository,
        IAiChatRepository aiChatRepository)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _aiChatRepository = aiChatRepository;
    }

    public IReadOnlyCollection<string> FunctionNames => new[] { "create_sale_draft", "create_purchase_draft" };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "create_sale_draft" => new
            {
                name = "create_sale_draft",
                description = "Buat draft transaksi penjualan POS.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productId = new { type = "string", description = "GUID dari produk (dari search_product)" },
                                    productName = new { type = "string", description = "Nama produk" },
                                    quantity = new { type = "number", description = "Jumlah yang dibeli" },
                                    unitPrice = new { type = "number", description = "Opsional. Biarkan kosong untuk menggunakan harga default DB" }
                                },
                                required = new[] { "quantity" }
                            }
                        },
                        paidAmount = new { type = "number", description = "Uang yang dibayarkan pembeli" }
                    },
                    required = new[] { "items" }
                }
            },
            "create_purchase_draft" => new
            {
                name = "create_purchase_draft",
                description = "Buat draft pembelian stok dari supplier.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        supplierId = new { type = "string", description = "GUID supplier (dari search_supplier)" },
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    productId = new { type = "string", description = "GUID produk" },
                                    quantity = new { type = "number", description = "Jumlah yang dibeli" },
                                    unitCost = new { type = "number", description = "Harga beli per satuan" }
                                },
                                required = new[] { "productId", "quantity", "unitCost" }
                            }
                        }
                    },
                    required = new[] { "supplierId", "items" }
                }
            },
            _ => throw new ArgumentException($"Unknown function: {functionName}")
        };
    }

    public async Task<object> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var root = context.Arguments;
        switch (context.FunctionName)
        {
            case "create_sale_draft":
                {
                    var itemsElement = root.GetProperty("items");
                    var paidAmount = root.TryGetProperty("paidAmount", out var paidEl) && paidEl.ValueKind == JsonValueKind.Number ? paidEl.GetDecimal() : 0m;

                    var resolvedItems = new List<object>();
                    var draftItemsPayload = new List<Dictionary<string, object>>();
                    decimal totalAmount = 0m;

                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        string? productIdStr = item.TryGetProperty("productId", out var idEl) ? idEl.GetString() : null;
                        string? productName = item.TryGetProperty("productName", out var nameEl) ? nameEl.GetString() : null;
                        var quantity = item.GetProperty("quantity").GetInt32();
                        decimal? customPrice = item.TryGetProperty("unitPrice", out var priceEl) && priceEl.ValueKind == JsonValueKind.Number ? priceEl.GetDecimal() : (decimal?)null;

                        Product? product = null;
                        if (!string.IsNullOrEmpty(productIdStr) && Guid.TryParse(productIdStr, out var prodId))
                        {
                            product = await _productRepository.GetByIdAsync(prodId, cancellationToken);
                        }
                        else if (!string.IsNullOrEmpty(productName))
                        {
                            var products = await _productRepository.GetAllAsync(productName, null, true, null, 1, 1, cancellationToken);
                            product = products.FirstOrDefault();
                        }

                        if (product == null)
                        {
                            return new { error = $"Produk '{productName ?? productIdStr}' tidak ditemukan di database." };
                        }

                        if (product.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                        }

                        decimal unitPrice = customPrice ?? product.SellingPrice;
                        decimal subtotal = quantity * unitPrice;
                        totalAmount += subtotal;

                        resolvedItems.Add(new
                        {
                            productId = product.Id,
                            productName = product.Name,
                            quantity = quantity,
                            unitPrice = unitPrice,
                            subtotal = subtotal,
                            hasPriceDifference = customPrice.HasValue && customPrice.Value != product.SellingPrice,
                            originalPrice = product.SellingPrice
                        });

                        draftItemsPayload.Add(new Dictionary<string, object>
                        {
                            { "productId", product.Id },
                            { "quantity", quantity },
                            { "unitPrice", unitPrice }
                        });
                    }

                    decimal change = paidAmount - totalAmount;

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_sale",
                        EntityType = "sale",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            items = draftItemsPayload,
                            paidAmount = paidAmount,
                            totalAmount = totalAmount,
                            change = change
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        items = resolvedItems,
                        totalAmount = totalAmount,
                        paidAmount = paidAmount,
                        change = change,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "create_purchase_draft":
                {
                    var supplierIdStr = root.GetProperty("supplierId").GetString();
                    if (string.IsNullOrEmpty(supplierIdStr) || !Guid.TryParse(supplierIdStr, out var supplierId))
                    {
                        return new { error = "SupplierId tidak valid." };
                    }

                    var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
                    if (supplier == null)
                    {
                        return new { error = "Supplier tidak ditemukan." };
                    }

                    if (supplier.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Supplier bukan milik toko ini.");
                    }

                    var itemsElement = root.GetProperty("items");
                    var resolvedItems = new List<object>();
                    var draftItemsPayload = new List<Dictionary<string, object>>();
                    decimal totalAmount = 0m;

                    // Extract and validate product IDs
                    var productIds = new List<Guid>();
                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        var productIdStr = item.GetProperty("productId").GetString();
                        if (string.IsNullOrEmpty(productIdStr) || !Guid.TryParse(productIdStr, out var productId))
                        {
                            return new { error = "ProductId tidak valid." };
                        }
                        productIds.Add(productId);
                    }

                    var productsDict = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
                        .ToDictionary(p => p.Id);

                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        var productIdStr = item.GetProperty("productId").GetString();
                        Guid.TryParse(productIdStr, out var productId);

                        if (!productsDict.TryGetValue(productId, out var product))
                        {
                            return new { error = $"Produk dengan ID {productId} tidak ditemukan." };
                        }

                        if (product.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                        }

                        var quantity = item.GetProperty("quantity").GetInt32();
                        var unitCost = item.GetProperty("unitCost").GetDecimal();
                        decimal subtotal = quantity * unitCost;
                        totalAmount += subtotal;

                        resolvedItems.Add(new
                        {
                            productId = product.Id,
                            productName = product.Name,
                            quantity = quantity,
                            unitCost = unitCost,
                            subtotal = subtotal
                        });

                        draftItemsPayload.Add(new Dictionary<string, object>
                        {
                            { "productId", product.Id },
                            { "quantity", quantity },
                            { "unitCost", unitCost }
                        });
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_purchase",
                        EntityType = "purchase",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            supplierId = supplierId,
                            items = draftItemsPayload,
                            totalAmount = totalAmount
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        supplierName = supplier.Name,
                        items = resolvedItems,
                        totalAmount = totalAmount,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
