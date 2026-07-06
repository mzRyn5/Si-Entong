using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Contracts.Responses.Reports;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        var salesQuery = _context.Sales
            .AsNoTracking()
            .Where(s => s.Status == TransactionStatus.Completed
                && s.SaleDate >= startDate
                && s.SaleDate < endDate);

        var totalSalesAmount = await salesQuery.SumAsync(s => s.TotalAmount, cancellationToken);
        var totalSalesTransactions = await salesQuery.CountAsync(cancellationToken);

        var totalPurchaseAmount = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status == TransactionStatus.Posted
                && p.PurchaseDate >= startDate
                && p.PurchaseDate < endDate)
            .SumAsync(p => p.TotalAmount, cancellationToken);

        var totalPurchaseReturnAmount = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= startDate
                && r.ReturnDate < endDate)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        var totalExpenseAmount = await _context.Expenses
            .AsNoTracking()
            .Where(e => (e.Status == TransactionStatus.Posted || e.Status == TransactionStatus.Completed)
                && e.ExpenseDate >= startDate
                && e.ExpenseDate < endDate)
            .SumAsync(e => e.Amount, cancellationToken);

        var estimatedCostOfGoodsSold = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == TransactionStatus.Completed
                && i.Sale.SaleDate >= startDate
                && i.Sale.SaleDate < endDate)
            .SumAsync(i => i.PurchasePriceSnapshot * i.Quantity, cancellationToken);

        var totalSalesReturnAmount = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= startDate
                && r.ReturnDate < endDate)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        var returnedCostOfGoodsSold = await _context.SalesReturnItems
            .AsNoTracking()
            .Where(i => i.SalesReturn.ReturnDate >= startDate
                && i.SalesReturn.ReturnDate < endDate)
            .SumAsync(i => i.Product.PurchasePrice * i.Quantity, cancellationToken);

        var netSalesAmount = totalSalesAmount - totalSalesReturnAmount;
        var netCostOfGoodsSold = estimatedCostOfGoodsSold - returnedCostOfGoodsSold;

        var lowStockProductCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive
                && p.CurrentStock > 0
                && p.CurrentStock <= p.LowStockThreshold, cancellationToken);

        var outOfStockProductCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive
                && p.CurrentStock <= 0, cancellationToken);

        var paymentMethodSummaries = await _context.Sales
            .AsNoTracking()
            .Where(s => s.Status == TransactionStatus.Completed
                && s.SaleDate >= startDate
                && s.SaleDate < endDate)
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new PaymentMethodSummaryResponse
            {
                PaymentMethod = g.Key.ToString(),
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var latestActivities = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new LatestActivityResponse
            {
                CreatedAt = a.CreatedAt,
                Module = a.Module,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                UserName = a.User.Name ?? a.User.Username ?? "System"
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryResponse
        {
            Date = startDate,
            TotalSalesAmount = netSalesAmount,
            TotalSalesTransactions = totalSalesTransactions,
            TotalPurchaseAmount = totalPurchaseAmount - totalPurchaseReturnAmount,
            TotalExpenseAmount = totalExpenseAmount,
            GrossProfitAmount = netSalesAmount - netCostOfGoodsSold,
            LowStockProductCount = lowStockProductCount,
            OutOfStockProductCount = outOfStockProductCount,
            PaymentMethodSummaries = paymentMethodSummaries,
            LatestActivities = latestActivities
        };
    }

    public async Task<IEnumerable<DailySalesReportResponse>> GetDailySalesAsync(
        DateTime fromDate,
        DateTime toDate,
        string? paymentMethod = null,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var query = _context.Sales
            .AsNoTracking()
            .Where(s => s.Status == TransactionStatus.Completed
                && s.SaleDate >= startDate
                && s.SaleDate < endDate);

        if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<PaymentMethod>(paymentMethod, true, out var pm))
        {
            query = query.Where(s => s.PaymentMethod == pm);
        }

        if (productId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.ProductId == productId.Value));
        }

        var sales = await query
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesReportResponse
            {
                Date = g.Key,
                TransactionCount = g.Count(),
                TotalSalesAmount = g.Sum(s => s.Subtotal),
                TotalDiscountAmount = g.Sum(s => s.DiscountAmount),
                NetSalesAmount = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var returnsQuery = _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= startDate
                && r.ReturnDate < endDate);

        if (productId.HasValue)
        {
            returnsQuery = returnsQuery.Where(r => r.Items.Any(i => i.ProductId == productId.Value));
        }

        if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<PaymentMethod>(paymentMethod, true, out var pmReturn)) { returnsQuery = returnsQuery.Where(r => r.Sale.PaymentMethod == pmReturn);
        }

        var returns = await returnsQuery
            .GroupBy(r => r.ReturnDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalRefundAmount = g.Sum(r => r.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        foreach (var salesDay in sales)
        {
            var refundAmount = returns.FirstOrDefault(r => r.Date == salesDay.Date.Date)?.TotalRefundAmount ?? 0;
            salesDay.NetSalesAmount -= refundAmount;
            salesDay.TotalSalesAmount -= refundAmount;
        }

        return sales;
    }

    public async Task<IEnumerable<PurchaseReportResponse>> GetPurchasesAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var query = _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status == TransactionStatus.Posted
                && p.PurchaseDate >= startDate
                && p.PurchaseDate < endDate);

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        var returnQuery = _context.PurchaseReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= startDate
                && r.ReturnDate < endDate);

        if (supplierId.HasValue)
        {
            returnQuery = returnQuery.Where(r => r.Purchase.SupplierId == supplierId.Value);
        }

        var returns = await returnQuery
            .GroupBy(r => r.PurchaseId)
            .Select(g => new
            {
                PurchaseId = g.Key,
                ReturnAmount = g.Sum(r => r.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var purchases = await query
            .OrderByDescending(p => p.PurchaseDate)
            .Select(p => new PurchaseReportResponse
            {
                PurchaseId = p.Id,
                PurchaseNumber = p.PurchaseNumber,
                PurchaseDate = p.PurchaseDate,
                SupplierName = p.Supplier.Name,
                GrossPurchaseAmount = p.TotalAmount,
                PaymentMethod = p.PaymentMethod.ToString()
            })
            .ToListAsync(cancellationToken);

        foreach (var p in purchases)
        {
            var ret = returns.FirstOrDefault(r => r.PurchaseId == p.PurchaseId);
            p.ReturnAmount = ret?.ReturnAmount ?? 0;
            p.NetPurchaseAmount = p.GrossPurchaseAmount - p.ReturnAmount;
        }

        return purchases;
    }

    public async Task<IEnumerable<ExpenseReportResponse>> GetExpensesAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var query = _context.Expenses
            .AsNoTracking()
            .Where(e => (e.Status == TransactionStatus.Posted || e.Status == TransactionStatus.Completed)
                && e.ExpenseDate >= startDate
                && e.ExpenseDate < endDate);

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseReportResponse
            {
                ExpenseId = e.Id,
                ExpenseNumber = e.ExpenseNumber,
                ExpenseDate = e.ExpenseDate,
                CategoryName = e.Category.Name,
                Amount = e.Amount,
                PaymentMethod = e.PaymentMethod.ToString(),
                Notes = e.Notes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovementReportResponse>> GetInventoryMovementsAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? productId,
        string? movementType,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var query = _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovementDate >= startDate
                && m.MovementDate < endDate);

        if (productId.HasValue)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        if (!string.IsNullOrEmpty(movementType) && Enum.TryParse<StockMovementType>(movementType, true, out var mt))
        {
            query = query.Where(m => m.MovementType == mt);
        }

        return await query
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new InventoryMovementReportResponse
            {
                MovementId = m.Id,
                MovementDate = m.MovementDate,
                ProductId = m.ProductId,
                ProductName = m.Product.Name,
                MovementType = m.MovementType.ToString(),
                QuantityBefore = m.QuantityBefore,
                QuantityChange = m.QuantityChange,
                QuantityAfter = m.QuantityAfter,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                Notes = m.Notes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductSalesReportResponse>> GetProductSalesAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var query = _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == TransactionStatus.Completed
                && i.Sale.SaleDate >= startDate
                && i.Sale.SaleDate < endDate);

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.Product.CategoryId == categoryId.Value);
        }

        var productSales = await query
            .GroupBy(i => new
            {
                i.ProductId,
                i.ProductSnapshotName
            })
            .OrderBy(g => g.Key.ProductSnapshotName)
            .Select(g => new ProductSalesReportResponse
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductSnapshotName,
                QuantitySold = g.Sum(i => i.Quantity),
                GrossSalesAmount = g.Sum(i => i.Subtotal),
                EstimatedCostAmount = g.Sum(i => i.PurchasePriceSnapshot * i.Quantity),
                EstimatedGrossProfitAmount = g.Sum(i => i.Subtotal - (i.PurchasePriceSnapshot * i.Quantity))
            })
            .ToListAsync(cancellationToken);

        var productReturns = await _context.SalesReturnItems
            .AsNoTracking()
            .Where(i => i.SalesReturn.ReturnDate >= startDate
                && i.SalesReturn.ReturnDate < endDate)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantityReturned = g.Sum(i => i.Quantity),
                RefundAmount = g.Sum(i => i.TotalPrice),
                ReturnedCostAmount = g.Sum(i => i.Product.PurchasePrice * i.Quantity)
            })
            .ToListAsync(cancellationToken);

        foreach (var productSale in productSales)
        {
            var productReturn = productReturns.FirstOrDefault(r => r.ProductId == productSale.ProductId);
            if (productReturn == null) continue;

            productSale.QuantitySold -= productReturn.QuantityReturned;
            productSale.GrossSalesAmount -= productReturn.RefundAmount;
            productSale.EstimatedCostAmount -= productReturn.ReturnedCostAmount;
            productSale.EstimatedGrossProfitAmount = productSale.GrossSalesAmount - productSale.EstimatedCostAmount;
        }

        return productSales;
    }

    public async Task<IEnumerable<StockValuationReportResponse>> GetStockValuationAsync(
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new StockValuationReportResponse
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                StockValue = p.CurrentStock * p.PurchasePrice,
                SellingValue = p.CurrentStock * p.SellingPrice,
                PotentialProfit = p.CurrentStock * (p.SellingPrice - p.PurchasePrice)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BasicProfitReportResponse> GetBasicProfitAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1);

        var netSalesAmount = await _context.Sales
            .AsNoTracking()
            .Where(s => s.Status == TransactionStatus.Completed
                && s.SaleDate >= startDate
                && s.SaleDate < endDate)
            .SumAsync(s => s.TotalAmount, cancellationToken);

        var salesReturnAmount = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= startDate
                && r.ReturnDate < endDate)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        var estimatedCostOfGoodsSold = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == TransactionStatus.Completed
                && i.Sale.SaleDate >= startDate
                && i.Sale.SaleDate < endDate)
            .SumAsync(i => i.PurchasePriceSnapshot * i.Quantity, cancellationToken);

        var returnedCostOfGoodsSold = await _context.SalesReturnItems
            .AsNoTracking()
            .Where(i => i.SalesReturn.ReturnDate >= startDate
                && i.SalesReturn.ReturnDate < endDate)
            .SumAsync(i => i.Product.PurchasePrice * i.Quantity, cancellationToken);

        var expenseAmount = await _context.Expenses
            .AsNoTracking()
            .Where(e => (e.Status == TransactionStatus.Posted || e.Status == TransactionStatus.Completed)
                && e.ExpenseDate >= startDate
                && e.ExpenseDate < endDate)
            .SumAsync(e => e.Amount, cancellationToken);

        var adjustedNetSalesAmount = netSalesAmount - salesReturnAmount;
        var adjustedCostOfGoodsSold = estimatedCostOfGoodsSold - returnedCostOfGoodsSold;
        var estimatedGrossProfit = adjustedNetSalesAmount - adjustedCostOfGoodsSold;

        return new BasicProfitReportResponse
        {
            FromDate = startDate,
            ToDate = toDate.Date,
            NetSalesAmount = adjustedNetSalesAmount,
            EstimatedCostOfGoodsSold = adjustedCostOfGoodsSold,
            EstimatedGrossProfit = estimatedGrossProfit,
            ExpenseAmount = expenseAmount,
            EstimatedNetProfit = estimatedGrossProfit - expenseAmount
        };
    }
}

