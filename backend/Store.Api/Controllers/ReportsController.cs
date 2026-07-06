using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Reports;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Reports;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "SysAdminOrOwnerOrAdmin")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;

    public ReportsController(
        IReportService reportService,
        IReportExportService reportExportService)
    {
        _reportService = reportService;
        _reportExportService = reportExportService;
    }

    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDashboardSummaryAsync(date, cancellationToken);
        return SuccessResponse(result, "Ringkasan dashboard berhasil diambil.");
    }

    [HttpGet("daily-sales")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DailySalesReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailySales(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? paymentMethod,
        [FromQuery] Guid? productId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDailySalesAsync(fromDate, toDate, paymentMethod, productId, cancellationToken);
        return SuccessResponse(result, "Laporan penjualan harian berhasil diambil.");
    }

    [HttpGet("product-sales")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductSalesReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductSales(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetProductSalesAsync(fromDate, toDate, categoryId, cancellationToken);
        return SuccessResponse(result, "Laporan penjualan produk berhasil diambil.");
    }

    [HttpGet("stock-valuation")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StockValuationReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockValuation(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockValuationAsync(categoryId, cancellationToken);
        return SuccessResponse(result, "Laporan valuasi stok berhasil diambil.");
    }

    [HttpGet("basic-profit")]
    [ProducesResponseType(typeof(ApiResponse<BasicProfitReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBasicProfit(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBasicProfitAsync(fromDate, toDate, cancellationToken);
        return SuccessResponse(result, "Laporan profit dasar berhasil diambil.");
    }

    [HttpGet("inventory-movements")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InventoryMovementReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryMovements(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? productId,
        [FromQuery] string? movementType,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetInventoryMovementsAsync(
            fromDate, toDate, productId, movementType, cancellationToken);
        return SuccessResponse(result, "Laporan mutasi stok berhasil diambil.");
    }

    [HttpGet("purchases")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PurchaseReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchases(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchasesAsync(fromDate, toDate, supplierId, cancellationToken);
        return SuccessResponse(result, "Laporan pembelian berhasil diambil.");
    }

    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ExpenseReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetExpensesAsync(fromDate, toDate, categoryId, cancellationToken);
        return SuccessResponse(result, "Laporan pengeluaran berhasil diambil.");
    }

    [HttpGet("daily-sales/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDailySales(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? paymentMethod,
        [FromQuery] Guid? productId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDailySalesAsync(fromDate, toDate, paymentMethod, productId, cancellationToken);
        return ExcelFile(_reportExportService.ExportDailySales(result), BuildFileName("daily-sales", fromDate, toDate));
    }

    [HttpGet("purchases/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPurchases(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchasesAsync(fromDate, toDate, supplierId, cancellationToken);
        return ExcelFile(_reportExportService.ExportPurchases(result), BuildFileName("purchases", fromDate, toDate));
    }

    [HttpGet("stock-valuation/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportStockValuation(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockValuationAsync(categoryId, cancellationToken);
        return ExcelFile(_reportExportService.ExportStockValuation(result), $"stock-valuation-{DateTime.UtcNow:yyyyMMdd}.xls");
    }

    [HttpGet("basic-profit/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBasicProfit(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBasicProfitAsync(fromDate, toDate, cancellationToken);
        return ExcelFile(_reportExportService.ExportBasicProfit(result), BuildFileName("basic-profit", fromDate, toDate));
    }

    private FileContentResult ExcelFile(byte[] content, string fileName)
    {
        return File(content, "application/vnd.ms-excel", fileName);
    }

    private static string BuildFileName(string reportName, DateTime? fromDate, DateTime? toDate)
    {
        var endDate = (toDate ?? DateTime.UtcNow).Date;
        var startDate = (fromDate ?? endDate.AddDays(-30)).Date;

        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        return $"{reportName}-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.xls";
    }
}
