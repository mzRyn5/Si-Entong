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

public sealed class InventoryMasterToolHandler : IAiToolHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly IAiChatRepository _aiChatRepository;

    public InventoryMasterToolHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitRepository unitRepository,
        IAiChatRepository aiChatRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitRepository = unitRepository;
        _aiChatRepository = aiChatRepository;
    }

    public IReadOnlyCollection<string> FunctionNames => new[]
    {
        "create_stock_adjustment_draft",
        "update_product_draft",
        "create_product_draft",
        "create_customer_draft",
        "create_supplier_draft"
    };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "create_stock_adjustment_draft" => new
            {
                name = "create_stock_adjustment_draft",
                description = "Buat draft koreksi stok aktual produk",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID produk" },
                        newStock = new { type = "number", description = "Jumlah stok aktual yang baru" },
                        reason = new { type = "string", description = "Alasan penyesuaian/koreksi stok" }
                    },
                    required = new[] { "productId", "newStock", "reason" }
                }
            },
            "update_product_draft" => new
            {
                name = "update_product_draft",
                description = "Buat draft perubahan data produk (seperti harga jual)",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID produk" },
                        sellingPrice = new { type = "number", description = "Harga jual baru (opsional)" },
                        reason = new { type = "string", description = "Alasan perubahan" }
                    },
                    required = new[] { "productId" }
                }
            },
            "create_product_draft" => new
            {
                name = "create_product_draft",
                description = "Buat draft penambahan produk master baru.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nama produk baru" },
                        sku = new { type = "string", description = "SKU unik produk. Opsional." },
                        barcode = new { type = "string", description = "Barcode produk. Opsional." },
                        sellingPrice = new { type = "number", description = "Harga jual produk ke pelanggan" },
                        purchasePrice = new { type = "number", description = "Harga beli/modal produk dari supplier" },
                        initialStock = new { type = "number", description = "Stok awal produk. Opsional." },
                        categoryName = new { type = "string", description = "Nama kategori produk (misal: Sembako, Minuman). Opsional." },
                        unitName = new { type = "string", description = "Nama satuan produk (misal: pcs, pack, dus). Opsional." }
                    },
                    required = new[] { "name", "sellingPrice", "purchasePrice" }
                }
            },
            "create_customer_draft" => new
            {
                name = "create_customer_draft",
                description = "Buat draft penambahan pelanggan master baru.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nama pelanggan baru" },
                        phone = new { type = "string", description = "Nomor telepon pelanggan. Opsional." },
                        address = new { type = "string", description = "Alamat pelanggan. Opsional." }
                    },
                    required = new[] { "name" }
                }
            },
            "create_supplier_draft" => new
            {
                name = "create_supplier_draft",
                description = "Buat draft penambahan supplier master baru.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nama supplier baru" },
                        phone = new { type = "string", description = "Nomor telepon supplier. Opsional." },
                        address = new { type = "string", description = "Alamat supplier. Opsional." }
                    },
                    required = new[] { "name" }
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
            case "create_stock_adjustment_draft":
                {
                    var productIdStr = root.GetProperty("productId").GetString();
                    if (string.IsNullOrEmpty(productIdStr) || !Guid.TryParse(productIdStr, out var productId))
                    {
                        return new { error = "ProductId tidak valid." };
                    }

                    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                    if (product == null)
                    {
                        return new { error = "Produk tidak ditemukan." };
                    }

                    if (product.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                    }

                    var newStock = root.GetProperty("newStock").GetInt32();
                    var reason = root.GetProperty("reason").GetString() ?? "Koreksi Stok AI";

                    int difference = newStock - product.CurrentStock;

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_stock_adjustment",
                        EntityType = "stock",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            productId = productId,
                            currentStock = product.CurrentStock,
                            newStock = newStock,
                            difference = difference,
                            reason = reason
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        productName = product.Name,
                        currentStock = product.CurrentStock,
                        newStock = newStock,
                        difference = difference,
                        reason = reason,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "update_product_draft":
                {
                    var productIdStr = root.GetProperty("productId").GetString();
                    if (string.IsNullOrEmpty(productIdStr) || !Guid.TryParse(productIdStr, out var productId))
                    {
                        return new { error = "ProductId tidak valid." };
                    }

                    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                    if (product == null)
                    {
                        return new { error = "Produk tidak ditemukan." };
                    }

                    if (product.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                    }

                    decimal? sellingPrice = root.TryGetProperty("sellingPrice", out var priceEl) && priceEl.ValueKind == JsonValueKind.Number ? priceEl.GetDecimal() : (decimal?)null;
                    var reason = root.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : "Update via AI";

                    if (sellingPrice.HasValue && sellingPrice.Value <= 0)
                    {
                        return new { error = "Harga jual baru tidak boleh kurang dari atau sama dengan 0." };
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "update_product",
                        EntityType = "product",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            productId = productId,
                            originalSellingPrice = product.SellingPrice,
                            newSellingPrice = sellingPrice,
                            reason = reason
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        productName = product.Name,
                        originalSellingPrice = product.SellingPrice,
                        newSellingPrice = sellingPrice,
                        reason = reason,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "create_product_draft":
                {
                    var name = root.GetProperty("name").GetString() ?? string.Empty;
                    var sellingPrice = root.GetProperty("sellingPrice").GetDecimal();
                    var purchasePrice = root.GetProperty("purchasePrice").GetDecimal();
                    var sku = root.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() ?? "" : "";
                    var barcode = root.TryGetProperty("barcode", out var bcEl) ? bcEl.GetString() : null;
                    var initialStock = root.TryGetProperty("initialStock", out var isEl) && isEl.ValueKind == JsonValueKind.Number ? isEl.GetInt32() : 0;
                    var categoryName = root.TryGetProperty("categoryName", out var catEl) ? catEl.GetString() ?? "" : "";
                    var unitName = root.TryGetProperty("unitName", out var unitEl) ? unitEl.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(name))
                    {
                        return new { error = "Nama produk tidak boleh kosong." };
                    }

                    // Resolve CategoryId
                    Guid categoryId = Guid.Empty;
                    string resolvedCategoryName = "Kategori Default";
                    if (!string.IsNullOrEmpty(categoryName))
                    {
                        var cats = await _categoryRepository.GetAllAsync(categoryName, true, cancellationToken);
                        var cat = cats.FirstOrDefault();
                        if (cat != null)
                        {
                            categoryId = cat.Id;
                            resolvedCategoryName = cat.Name;
                        }
                    }
                    if (categoryId == Guid.Empty)
                    {
                        var cats = await _categoryRepository.GetAllAsync(null, true, cancellationToken);
                        var cat = cats.FirstOrDefault();
                        if (cat != null)
                        {
                            categoryId = cat.Id;
                            resolvedCategoryName = cat.Name;
                        }
                        else
                        {
                            return new { error = "Kategori tidak ditemukan di database. Harap buat kategori terlebih dahulu." };
                        }
                    }

                    // Resolve UnitId
                    Guid unitId = Guid.Empty;
                    string resolvedUnitName = "pcs";
                    if (!string.IsNullOrEmpty(unitName))
                    {
                        var units = await _unitRepository.GetAllAsync(unitName, true, cancellationToken);
                        var unit = units.FirstOrDefault();
                        if (unit != null)
                        {
                            unitId = unit.Id;
                            resolvedUnitName = unit.Name;
                        }
                    }
                    if (unitId == Guid.Empty)
                    {
                        var units = await _unitRepository.GetAllAsync(null, true, cancellationToken);
                        var unit = units.FirstOrDefault();
                        if (unit != null)
                        {
                            unitId = unit.Id;
                            resolvedUnitName = unit.Name;
                        }
                        else
                        {
                            return new { error = "Satuan tidak ditemukan di database. Harap buat satuan terlebih dahulu." };
                        }
                    }

                    if (string.IsNullOrEmpty(sku))
                    {
                        sku = "PRD-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_product",
                        EntityType = "product",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            name = name,
                            sku = sku,
                            barcode = barcode,
                            sellingPrice = sellingPrice,
                            purchasePrice = purchasePrice,
                            initialStock = initialStock,
                            categoryId = categoryId,
                            unitId = unitId
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        name = name,
                        sku = sku,
                        barcode = barcode,
                        sellingPrice = sellingPrice,
                        purchasePrice = purchasePrice,
                        initialStock = initialStock,
                        categoryName = resolvedCategoryName,
                        unitName = resolvedUnitName,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "create_customer_draft":
                {
                    var name = root.GetProperty("name").GetString() ?? string.Empty;
                    var phone = root.TryGetProperty("phone", out var phEl) ? phEl.GetString() : null;
                    var address = root.TryGetProperty("address", out var adEl) ? adEl.GetString() : null;

                    if (string.IsNullOrEmpty(name))
                    {
                        return new { error = "Nama pelanggan tidak boleh kosong." };
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_customer",
                        EntityType = "customer",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            name = name,
                            phone = phone,
                            address = address
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        name = name,
                        phone = phone,
                        address = address,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "create_supplier_draft":
                {
                    var name = root.GetProperty("name").GetString() ?? string.Empty;
                    var phone = root.TryGetProperty("phone", out var phEl) ? phEl.GetString() : null;
                    var address = root.TryGetProperty("address", out var adEl) ? adEl.GetString() : null;

                    if (string.IsNullOrEmpty(name))
                    {
                        return new { error = "Nama supplier tidak boleh kosong." };
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_supplier",
                        EntityType = "supplier",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            name = name,
                            phone = phone,
                            address = address
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        name = name,
                        phone = phone,
                        address = address,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
