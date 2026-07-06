using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Reports;
using Store.Contracts.Responses.Reports;
using Xunit;

namespace Store.Tests.UnitTests.Reports;

public class ReportServiceTests
{
    [Fact]
    public async Task GetDailySalesAsync_WhenDatesAreReversed_NormalizesDateRangeBeforeQueryingRepository()
    {
        var reportRepository = new Mock<IReportRepository>();
        var expectedFromDate = new DateTime(2026, 6, 1);
        var expectedToDate = new DateTime(2026, 6, 15);

        reportRepository
            .Setup(x => x.GetDailySalesAsync(
                expectedFromDate,
                expectedToDate,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySalesReportResponse>
            {
                new()
                {
                    Date = expectedFromDate,
                    TransactionCount = 1,
                    NetSalesAmount = 24_000
                }
            });

        var service = new ReportService(reportRepository.Object);

        var result = await service.GetDailySalesAsync(expectedToDate, expectedFromDate);

        result.Should().ContainSingle();
        reportRepository.Verify(x => x.GetDailySalesAsync(
            expectedFromDate,
            expectedToDate,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDailySalesAsync_WithFilters_PassesPaymentMethodAndProductIdToRepository()
    {
        var reportRepository = new Mock<IReportRepository>();
        var fromDate = new DateTime(2026, 6, 1);
        var toDate = new DateTime(2026, 6, 15);
        var productId = Guid.NewGuid();

        reportRepository
            .Setup(x => x.GetDailySalesAsync(
                fromDate,
                toDate,
                "Cash",
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySalesReportResponse>());

        var service = new ReportService(reportRepository.Object);

        await service.GetDailySalesAsync(fromDate, toDate, "Cash", productId);

        reportRepository.Verify(x => x.GetDailySalesAsync(
            fromDate,
            toDate,
            "Cash",
            productId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ReportExportService_ExportDailySales_CreatesExcelCompatibleWorkbook()
    {
        var service = new ReportExportService();

        var content = service.ExportDailySales([
            new DailySalesReportResponse
            {
                Date = new DateTime(2026, 6, 15),
                TransactionCount = 2,
                TotalSalesAmount = 100_000,
                TotalDiscountAmount = 5_000,
                NetSalesAmount = 95_000
            }
        ]);

        var xml = System.Text.Encoding.UTF8.GetString(content);
        xml.Should().Contain("Workbook");
        xml.Should().Contain("Daily Sales");
        xml.Should().Contain("Net Sales");
        xml.Should().Contain("95000");
    }
}
