using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Receivables;
using Store.Contracts.Requests.Receivables;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Xunit;

namespace Store.Tests.UnitTests.Receivables;

public class ReceivableServiceTests
{
    [Fact]
    public async Task RecordPaymentAsync_ShouldAddPaymentToReceivableCollectionAndReturnInResponse()
    {
        // Arrange
        var receivableId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var receivable = new Receivable
        {
            Id = receivableId,
            ReceivableNumber = "REC-0001",
            TotalAmount = 150_000,
            PaidAmount = 0,
            RemainingAmount = 150_000,
            PaymentStatus = PaymentStatus.Unpaid,
            DueDate = DateTime.UtcNow.AddDays(14),
            Payments = new List<ReceivablePayment>()
        };

        var receivableRepositoryMock = new Mock<IReceivableRepository>();
        receivableRepositoryMock
            .Setup(x => x.GetByIdAsync(receivableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receivable);

        receivableRepositoryMock
            .Setup(x => x.AddPaymentAsync(It.IsAny<ReceivablePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        receivableRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Receivable>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        auditLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ReceivableService(receivableRepositoryMock.Object, auditLogRepositoryMock.Object);

        var request = new RecordReceivablePaymentRequest
        {
            PaymentDate = DateTime.UtcNow,
            Amount = 50_000,
            PaymentMethod = "QRIS",
            Notes = "Partial payment"
        };

        // Act
        var response = await service.RecordPaymentAsync(receivableId, request, userId);

        // Assert
        response.Should().NotBeNull();
        response.PaidAmount.Should().Be(50_000);
        response.RemainingAmount.Should().Be(100_000);
        response.PaymentStatus.Should().Be(PaymentStatus.Partial.ToString());
        response.Payments.Should().HaveCount(1);
        response.Payments[0].Amount.Should().Be(50_000);
        response.Payments[0].PaymentMethod.Should().Be("QRIS");
        response.Payments[0].Notes.Should().Be("Partial payment");
        
        // Verify payment is saved
        receivableRepositoryMock.Verify(x => x.AddPaymentAsync(It.Is<ReceivablePayment>(p => 
            p.ReceivableId == receivableId && 
            p.Amount == 50_000 && 
            p.PaymentMethod == PaymentMethod.QRIS && 
            p.Notes == "Partial payment"
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify receivable is updated
        receivableRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Receivable>(r => 
            r.Id == receivableId && 
            r.PaidAmount == 50_000 && 
            r.RemainingAmount == 100_000 && 
            r.PaymentStatus == PaymentStatus.Partial
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
