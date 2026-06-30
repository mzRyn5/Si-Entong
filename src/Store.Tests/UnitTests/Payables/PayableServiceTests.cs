using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Payables;
using Store.Contracts.Requests.Payables;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Xunit;

namespace Store.Tests.UnitTests.Payables;

public class PayableServiceTests
{
    [Fact]
    public async Task RecordPaymentAsync_ShouldAddPaymentToPayableCollectionAndReturnInResponse()
    {
        // Arrange
        var payableId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var payable = new Payable
        {
            Id = payableId,
            PayableNumber = "PAY-0001",
            TotalAmount = 100_000,
            PaidAmount = 0,
            RemainingAmount = 100_000,
            PaymentStatus = PaymentStatus.Unpaid,
            DueDate = DateTime.UtcNow.AddDays(7),
            Payments = new List<PayablePayment>()
        };

        var payableRepositoryMock = new Mock<IPayableRepository>();
        payableRepositoryMock
            .Setup(x => x.GetByIdAsync(payableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payable);

        payableRepositoryMock
            .Setup(x => x.AddPaymentAsync(It.IsAny<PayablePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        payableRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payable>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        auditLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new PayableService(payableRepositoryMock.Object, auditLogRepositoryMock.Object);

        var request = new RecordPayablePaymentRequest
        {
            PaymentDate = DateTime.UtcNow,
            Amount = 40_000,
            PaymentMethod = "Transfer",
            Notes = "Dp payment"
        };

        // Act
        var response = await service.RecordPaymentAsync(payableId, request, userId);

        // Assert
        response.Should().NotBeNull();
        response.PaidAmount.Should().Be(40_000);
        response.RemainingAmount.Should().Be(60_000);
        response.PaymentStatus.Should().Be(PaymentStatus.Partial.ToString());
        response.Payments.Should().HaveCount(1);
        response.Payments[0].Amount.Should().Be(40_000);
        response.Payments[0].PaymentMethod.Should().Be("Transfer");
        response.Payments[0].Notes.Should().Be("Dp payment");
        
        // Verify payment is saved
        payableRepositoryMock.Verify(x => x.AddPaymentAsync(It.Is<PayablePayment>(p => 
            p.PayableId == payableId && 
            p.Amount == 40_000 && 
            p.PaymentMethod == PaymentMethod.Transfer && 
            p.Notes == "Dp payment"
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify payable is updated
        payableRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Payable>(p => 
            p.Id == payableId && 
            p.PaidAmount == 40_000 && 
            p.RemainingAmount == 60_000 && 
            p.PaymentStatus == PaymentStatus.Partial
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
