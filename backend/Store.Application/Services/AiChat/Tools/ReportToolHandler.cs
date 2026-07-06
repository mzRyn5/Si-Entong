using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Services.Reports;

namespace Store.Application.Services.AiChat.Tools;

public sealed class ReportToolHandler : IAiToolHandler
{
    private readonly IReportService _reportService;

    public ReportToolHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public IReadOnlyCollection<string> FunctionNames => new[]
    {
        "get_dashboard_summary",
        "get_daily_sales_report",
        "get_top_selling_products",
        "get_profit_report",
        "get_expense_summary",
        "get_stock_valuation"
    };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "get_dashboard_summary" => new
            {
                name = "get_dashboard_summary",
                description = "Dapatkan ringkasan dashboard toko: total penjualan, pembelian, pengeluaran, laba kotor, stok rendah. Bisa untuk tanggal tertentu atau hari ini.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        date = new { type = "string", description = "Tanggal dalam format YYYY-MM-DD. Kosongkan untuk hari ini." }
                    },
                    required = System.Array.Empty<string>()
                }
            },
            "get_daily_sales_report" => new
            {
                name = "get_daily_sales_report",
                description = "Dapatkan laporan penjualan per hari dalam rentang tanggal. Gunakan untuk menjawab pertanyaan seperti 'berapa penjualan minggu ini', 'tren penjualan bulan ini'.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        fromDate = new { type = "string", description = "Tanggal awal (YYYY-MM-DD). Default: 7 hari lalu." },
                        toDate = new { type = "string", description = "Tanggal akhir (YYYY-MM-DD). Default: hari ini." }
                    },
                    required = System.Array.Empty<string>()
                }
            },
            "get_top_selling_products" => new
            {
                name = "get_top_selling_products",
                description = "Dapatkan daftar produk terlaris (paling banyak terjual) dalam rentang tanggal tertentu, beserta qty dan estimasi profit.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        fromDate = new { type = "string", description = "Tanggal awal (YYYY-MM-DD). Default: awal bulan ini." },
                        toDate = new { type = "string", description = "Tanggal akhir (YYYY-MM-DD). Default: hari ini." },
                        limit = new { type = "number", description = "Jumlah produk yang ditampilkan. Default: 10." }
                    },
                    required = System.Array.Empty<string>()
                }
            },
            "get_profit_report" => new
            {
                name = "get_profit_report",
                description = "Dapatkan ringkasan laba rugi: penjualan bersih, HPP, laba kotor, pengeluaran, laba bersih. Gunakan untuk menjawab 'untung berapa bulan ini', 'profit margin'.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        fromDate = new { type = "string", description = "Tanggal awal. Default: awal bulan ini." },
                        toDate = new { type = "string", description = "Tanggal akhir. Default: hari ini." }
                    },
                    required = System.Array.Empty<string>()
                }
            },
            "get_expense_summary" => new
            {
                name = "get_expense_summary",
                description = "Dapatkan ringkasan pengeluaran dalam rentang tanggal, termasuk breakdown per kategori.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        fromDate = new { type = "string", description = "Tanggal awal. Default: awal bulan ini." },
                        toDate = new { type = "string", description = "Tanggal akhir. Default: hari ini." }
                    },
                    required = System.Array.Empty<string>()
                }
            },
            "get_stock_valuation" => new
            {
                name = "get_stock_valuation",
                description = "Dapatkan total nilai stok barang saat ini (harga beli × jumlah stok). Gunakan untuk menjawab 'berapa nilai inventaris toko'.",
                parameters = new
                {
                    type = "object",
                    properties = new System.Collections.Generic.Dictionary<string, object>()
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
            case "get_dashboard_summary":
                {
                    DateTime? date = null;
                    if (root.TryGetProperty("date", out var dateEl) && !string.IsNullOrEmpty(dateEl.GetString()))
                    {
                        if (DateTime.TryParse(dateEl.GetString(), out var parsedDate))
                        {
                            date = parsedDate;
                        }
                    }

                    var summary = await _reportService.GetDashboardSummaryAsync(date, cancellationToken);
                    return new
                    {
                        date = (date ?? DateTime.UtcNow).ToString("yyyy-MM-dd"),
                        totalSalesAmount = summary.TotalSalesAmount,
                        totalSalesTransactions = summary.TotalSalesTransactions,
                        totalPurchaseAmount = summary.TotalPurchaseAmount,
                        totalExpenseAmount = summary.TotalExpenseAmount,
                        grossProfitAmount = summary.GrossProfitAmount,
                        lowStockProductCount = summary.LowStockProductCount,
                        outOfStockProductCount = summary.OutOfStockProductCount,
                        paymentMethods = summary.PaymentMethodSummaries.Select(p => new
                        {
                            method = p.PaymentMethod,
                            count = p.TransactionCount,
                            amount = p.TotalAmount
                        }).ToList()
                    };
                }

            case "get_daily_sales_report":
                {
                    DateTime? fromDate = DateTime.UtcNow.AddDays(-7).Date;
                    DateTime? toDate = DateTime.UtcNow.Date;

                    if (root.TryGetProperty("fromDate", out var fromEl) && !string.IsNullOrEmpty(fromEl.GetString()))
                    {
                        if (DateTime.TryParse(fromEl.GetString(), out var parsedFrom))
                        {
                            fromDate = parsedFrom;
                        }
                    }
                    if (root.TryGetProperty("toDate", out var toEl) && !string.IsNullOrEmpty(toEl.GetString()))
                    {
                        if (DateTime.TryParse(toEl.GetString(), out var parsedTo))
                        {
                            toDate = parsedTo;
                        }
                    }

                    var sales = await _reportService.GetDailySalesAsync(fromDate, toDate, null, null, cancellationToken);
                    return new
                    {
                        fromDate = fromDate?.ToString("yyyy-MM-dd"),
                        toDate = toDate?.ToString("yyyy-MM-dd"),
                        dailySales = sales.Select(s => new
                        {
                            date = s.Date.ToString("yyyy-MM-dd"),
                            transactionCount = s.TransactionCount,
                            totalAmount = s.TotalSalesAmount
                        }).ToList()
                    };
                }

            case "get_top_selling_products":
                {
                    DateTime? fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    DateTime? toDate = DateTime.UtcNow.Date;
                    int limit = 10;

                    if (root.TryGetProperty("fromDate", out var fromEl) && !string.IsNullOrEmpty(fromEl.GetString()))
                    {
                        if (DateTime.TryParse(fromEl.GetString(), out var parsedFrom))
                        {
                            fromDate = parsedFrom;
                        }
                    }
                    if (root.TryGetProperty("toDate", out var toEl) && !string.IsNullOrEmpty(toEl.GetString()))
                    {
                        if (DateTime.TryParse(toEl.GetString(), out var parsedTo))
                        {
                            toDate = parsedTo;
                        }
                    }
                    if (root.TryGetProperty("limit", out var limEl) && limEl.ValueKind == JsonValueKind.Number)
                    {
                        limit = limEl.GetInt32();
                    }

                    var productSales = await _reportService.GetProductSalesAsync(fromDate, toDate, null, cancellationToken);
                    var topProducts = productSales
                        .OrderByDescending(p => p.QuantitySold)
                        .Take(limit)
                        .Select(p => new
                        {
                            productName = p.ProductName,
                            quantitySold = p.QuantitySold,
                            grossSalesAmount = p.GrossSalesAmount,
                            estimatedProfit = p.EstimatedGrossProfitAmount
                        }).ToList();

                    return new
                    {
                        fromDate = fromDate?.ToString("yyyy-MM-dd"),
                        toDate = toDate?.ToString("yyyy-MM-dd"),
                        topProducts = topProducts
                    };
                }

            case "get_profit_report":
                {
                    DateTime? fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    DateTime? toDate = DateTime.UtcNow.Date;

                    if (root.TryGetProperty("fromDate", out var fromEl) && !string.IsNullOrEmpty(fromEl.GetString()))
                    {
                        if (DateTime.TryParse(fromEl.GetString(), out var parsedFrom))
                        {
                            fromDate = parsedFrom;
                        }
                    }
                    if (root.TryGetProperty("toDate", out var toEl) && !string.IsNullOrEmpty(toEl.GetString()))
                    {
                        if (DateTime.TryParse(toEl.GetString(), out var parsedTo))
                        {
                            toDate = parsedTo;
                        }
                    }

                    var profit = await _reportService.GetBasicProfitAsync(fromDate, toDate, cancellationToken);
                    return new
                    {
                        fromDate = profit.FromDate.ToString("yyyy-MM-dd"),
                        toDate = profit.ToDate.ToString("yyyy-MM-dd"),
                        netSalesAmount = profit.NetSalesAmount,
                        estimatedCostOfGoodsSold = profit.EstimatedCostOfGoodsSold,
                        estimatedGrossProfit = profit.EstimatedGrossProfit,
                        expenseAmount = profit.ExpenseAmount,
                        estimatedNetProfit = profit.EstimatedNetProfit,
                        grossProfitMargin = profit.NetSalesAmount > 0
                            ? Math.Round(profit.EstimatedGrossProfit / profit.NetSalesAmount * 100, 1)
                            : 0
                    };
                }

            case "get_expense_summary":
                {
                    DateTime? fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    DateTime? toDate = DateTime.UtcNow.Date;

                    if (root.TryGetProperty("fromDate", out var fromEl) && !string.IsNullOrEmpty(fromEl.GetString()))
                    {
                        if (DateTime.TryParse(fromEl.GetString(), out var parsedFrom))
                        {
                            fromDate = parsedFrom;
                        }
                    }
                    if (root.TryGetProperty("toDate", out var toEl) && !string.IsNullOrEmpty(toEl.GetString()))
                    {
                        if (DateTime.TryParse(toEl.GetString(), out var parsedTo))
                        {
                            toDate = parsedTo;
                        }
                    }

                    var expenses = await _reportService.GetExpensesAsync(fromDate, toDate, null, cancellationToken);
                    
                    var groupedExpenses = expenses
                        .GroupBy(e => e.CategoryName)
                        .Select(g => new
                        {
                            categoryName = g.Key,
                            totalAmount = g.Sum(e => e.Amount)
                        }).ToList();

                    return new
                    {
                        fromDate = fromDate?.ToString("yyyy-MM-dd"),
                        toDate = toDate?.ToString("yyyy-MM-dd"),
                        totalExpense = expenses.Sum(e => e.Amount),
                        categories = groupedExpenses
                    };
                }

            case "get_stock_valuation":
                {
                    var valuation = await _reportService.GetStockValuationAsync(null, cancellationToken);
                    var valuationList = valuation.ToList();
                    return new
                    {
                        totalProductCount = valuationList.Count,
                        totalStockQuantity = valuationList.Sum(v => v.CurrentStock),
                        totalCostValue = valuationList.Sum(v => v.StockValue),
                        totalSellingValue = valuationList.Sum(v => v.SellingValue),
                        potentialProfit = valuationList.Sum(v => v.PotentialProfit)
                    };
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
