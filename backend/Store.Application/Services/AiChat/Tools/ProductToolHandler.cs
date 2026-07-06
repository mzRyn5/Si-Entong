using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.AiChat.Tools;

public sealed class ProductToolHandler : IAiToolHandler
{
    private readonly IProductRepository _productRepository;

    public ProductToolHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public IReadOnlyCollection<string> FunctionNames => new[] { "search_product", "check_stock", "get_low_stock" };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "search_product" => new
            {
                name = "search_product",
                description = "Cari produk berdasarkan nama, barcode, atau SKU di database",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        keyword = new { type = "string", description = "Nama produk, SKU, atau barcode" }
                    },
                    required = new[] { "keyword" }
                }
            },
            "check_stock" => new
            {
                name = "check_stock",
                description = "Mengecek jumlah stok produk",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        keyword = new { type = "string", description = "Nama produk yang ingin dicek stoknya" }
                    },
                    required = new[] { "keyword" }
                }
            },
            "get_low_stock" => new
            {
                name = "get_low_stock",
                description = "Dapatkan daftar produk yang memiliki stok rendah",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>()
                }
            },
            _ => throw new ArgumentException($"Unknown function: {functionName}")
        };
    }

    public async Task<object> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        switch (context.FunctionName)
        {
            case "search_product":
                {
                    var keyword = context.Arguments.GetProperty("keyword").GetString() ?? string.Empty;
                    var products = await _productRepository.GetAllAsync(keyword, null, true, null, 1, 10, cancellationToken);
                    
                    // Validate tenant ownership
                    foreach (var p in products)
                    {
                        if (p.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                        }
                    }

                    return products.Select(p => new
                    {
                        productId = p.Id,
                        sku = p.Sku,
                        barcode = p.Barcode,
                        name = p.Name,
                        sellingPrice = p.SellingPrice,
                        currentStock = p.CurrentStock,
                        unit = p.Unit?.Name ?? "pcs"
                    }).ToList();
                }

            case "check_stock":
                {
                    var keyword = context.Arguments.GetProperty("keyword").GetString() ?? string.Empty;
                    var products = await _productRepository.GetAllAsync(keyword, null, true, null, 1, 10, cancellationToken);

                    foreach (var p in products)
                    {
                        if (p.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                        }
                    }

                    return products.Select(p => new
                    {
                        productId = p.Id,
                        name = p.Name,
                        currentStock = p.CurrentStock,
                        unit = p.Unit?.Name ?? "pcs",
                        isLowStock = p.IsLowStock
                    }).ToList();
                }

            case "get_low_stock":
                {
                    var products = await _productRepository.GetLowStockProductsAsync(1, 20, cancellationToken);
                    
                    foreach (var p in products)
                    {
                        if (p.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Produk bukan milik toko ini.");
                        }
                    }

                    return products.Select(p => new
                    {
                        productId = p.Id,
                        name = p.Name,
                        currentStock = p.CurrentStock,
                        threshold = p.LowStockThreshold,
                        unit = p.Unit?.Name ?? "pcs"
                    }).ToList();
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
