using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.AiChat;
using Store.Application.Services.AiChat.Tools;
using Store.Application.Services.Reports;
using Store.Contracts.Responses.Reports;
using Xunit;

namespace Store.Tests.UnitTests.AiChat;

public class AiToolExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_GetStockValuation_ReturnsCostSellingAndPotentialProfitTotals()
    {
        var reportService = new Mock<IReportService>();
        reportService
            .Setup(x => x.GetStockValuationAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockValuationReportResponse>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Beras",
                    CurrentStock = 2,
                    PurchasePrice = 10_000,
                    SellingPrice = 12_500,
                    StockValue = 20_000,
                    SellingValue = 25_000,
                    PotentialProfit = 5_000
                },
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Gula",
                    CurrentStock = 3,
                    PurchasePrice = 8_000,
                    SellingPrice = 10_000,
                    StockValue = 24_000,
                    SellingValue = 30_000,
                    PotentialProfit = 6_000
                }
            });

        var handlers = new List<IAiToolHandler>
        {
            new ReportToolHandler(reportService.Object)
        };
        var registry = new AiToolRegistry(handlers);
        var executor = new AiToolExecutor(registry);

        var result = await executor.ExecuteAsync(
            Guid.NewGuid(),
            "get_stock_valuation",
            "{}",
            Guid.NewGuid(),
            Guid.NewGuid());

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = doc.RootElement;

        root.GetProperty("totalProductCount").GetInt32().Should().Be(2);
        root.GetProperty("totalStockQuantity").GetInt32().Should().Be(5);
        root.GetProperty("totalCostValue").GetDecimal().Should().Be(44_000);
        root.GetProperty("totalSellingValue").GetDecimal().Should().Be(55_000);
        root.GetProperty("potentialProfit").GetDecimal().Should().Be(11_000);
    }
}
