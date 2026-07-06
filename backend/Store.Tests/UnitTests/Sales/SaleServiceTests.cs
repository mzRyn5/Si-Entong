using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Sales;
using Store.Contracts.Requests.Sales;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Xunit;

namespace Store.Tests.UnitTests.Sales;

public class SaleServiceTests
{
    [Fact]
    public async Task CreateAsync_CashSale_ReducesStockAndRecordsStockMovement()
    {
        var cashierId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Gula Pasir",
            Sku = "SKU-001",
            PurchasePrice = 10_000,
            SellingPrice = 12_000,
            CurrentStock = 10,
            IsActive = true
        };
        var stockMovements = new List<StockMovement>();
        Sale? createdSale = null;

        var saleRepository = new Mock<ISaleRepository>();
        saleRepository
            .Setup(x => x.GenerateSaleNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SAL-20260615-0001");
        saleRepository
            .Setup(x => x.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale sale, CancellationToken _) =>
            {
                sale.Id = saleId;
                createdSale = sale;
                return sale;
            });
        saleRepository
            .Setup(x => x.GetByIdAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdSale);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        productRepository
            .Setup(x => x.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var stockMovementRepository = new Mock<IStockMovementRepository>();
        stockMovementRepository
            .Setup(x => x.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((movement, _) => stockMovements.Add(movement))
            .Returns(Task.CompletedTask);

        var storeSettingsRepository = new Mock<IStoreSettingsRepository>();
        storeSettingsRepository
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreSettings
            {
                AllowNegativeStock = false,
                RequireCashSessionForSales = false
            });

        var auditLogRepository = new Mock<IAuditLogRepository>();
        auditLogRepository
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SaleService(
            saleRepository.Object,
            productRepository.Object,
            Mock.Of<ICustomerRepository>(),
            Mock.Of<ICashSessionRepository>(),
            stockMovementRepository.Object,
            storeSettingsRepository.Object,
            Mock.Of<IStoreProfileRepository>(),
            auditLogRepository.Object,
            Mock.Of<IReceivableRepository>());

        var result = await service.CreateAsync(new CreateSaleRequest
        {
            SaleDate = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc),
            PaymentMethod = PaymentMethod.Cash,
            AmountPaid = 30_000,
            Items =
            [
                new CreateSaleItemRequest
                {
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 12_000
                }
            ]
        }, cashierId);

        result.TotalAmount.Should().Be(24_000);
        result.ChangeAmount.Should().Be(6_000);
        product.CurrentStock.Should().Be(8);
        stockMovements.Should().ContainSingle();
        stockMovements[0].MovementType.Should().Be(StockMovementType.Sale);
        stockMovements[0].QuantityBefore.Should().Be(10);
        stockMovements[0].QuantityChange.Should().Be(-2);
        stockMovements[0].QuantityAfter.Should().Be(8);
    }
}
