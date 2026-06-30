using Store.Application.Abstractions.Repositories;
using Store.Contracts.Responses.Reports;

namespace Store.Application.Services.Reports;

public interface IReportService
{
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DateTime? date, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailySalesReportResponse>> GetDailySalesAsync(DateTime? fromDate, DateTime? toDate, string? paymentMethod = null, Guid? productId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSalesReportResponse>> GetProductSalesAsync(DateTime? fromDate, DateTime? toDate, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockValuationReportResponse>> GetStockValuationAsync(Guid? categoryId, CancellationToken cancellationToken = default);
    Task<BasicProfitReportResponse> GetBasicProfitAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReportResponse>> GetPurchasesAsync(DateTime? fromDate, DateTime? toDate, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseReportResponse>> GetExpensesAsync(DateTime? fromDate, DateTime? toDate, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovementReportResponse>> GetInventoryMovementsAsync(DateTime? fromDate, DateTime? toDate, Guid? productId, string? movementType, CancellationToken cancellationToken = default);
}

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        DateTime? date,
        CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        return _reportRepository.GetDashboardSummaryAsync(targetDate, cancellationToken);
    }

    public Task<IEnumerable<DailySalesReportResponse>> GetDailySalesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? paymentMethod = null,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetDailySalesAsync(range.FromDate, range.ToDate, paymentMethod, productId, cancellationToken);
    }

    public Task<IEnumerable<ProductSalesReportResponse>> GetProductSalesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetProductSalesAsync(range.FromDate, range.ToDate, categoryId, cancellationToken);
    }

    public Task<IEnumerable<StockValuationReportResponse>> GetStockValuationAsync(
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        return _reportRepository.GetStockValuationAsync(categoryId, cancellationToken);
    }

    public Task<BasicProfitReportResponse> GetBasicProfitAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetBasicProfitAsync(range.FromDate, range.ToDate, cancellationToken);
    }

    public Task<IEnumerable<PurchaseReportResponse>> GetPurchasesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetPurchasesAsync(range.FromDate, range.ToDate, supplierId, cancellationToken);
    }

    public Task<IEnumerable<ExpenseReportResponse>> GetExpensesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetExpensesAsync(range.FromDate, range.ToDate, categoryId, cancellationToken);
    }

    public Task<IEnumerable<InventoryMovementReportResponse>> GetInventoryMovementsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? productId,
        string? movementType,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeDateRange(fromDate, toDate);
        return _reportRepository.GetInventoryMovementsAsync(range.FromDate, range.ToDate, productId, movementType, cancellationToken);
    }

    private static (DateTime FromDate, DateTime ToDate) NormalizeDateRange(DateTime? fromDate, DateTime? toDate)
    {
        var endDate = (toDate ?? DateTime.UtcNow).Date;
        var startDate = (fromDate ?? endDate.AddDays(-30)).Date;

        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        return (startDate, endDate);
    }
}
