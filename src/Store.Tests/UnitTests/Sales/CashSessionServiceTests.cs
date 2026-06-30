using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Sales;
using Store.Contracts.Requests.CashSessions;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using Xunit;

namespace Store.Tests.UnitTests.Sales;

public class CashSessionServiceTests
{
    [Fact]
    public async Task OpenAsync_WhenCashierAlreadyHasActiveSession_ThrowsBusinessRuleException()
    {
        var cashierId = Guid.NewGuid();
        var cashSessionRepository = new Mock<ICashSessionRepository>();
        cashSessionRepository
            .Setup(x => x.GetActiveByCashierIdAsync(cashierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CashSession
            {
                Id = Guid.NewGuid(),
                CashierId = cashierId,
                Status = CashSessionStatus.Open
            });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(cashierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = cashierId,
                Name = "Admin Toko",
                Username = "admin",
                Role = UserRole.Admin,
                IsActive = true
            });

        var service = new CashSessionService(
            cashSessionRepository.Object,
            userRepository.Object,
            Mock.Of<IAuditLogRepository>());

        var act = () => service.OpenAsync(new OpenCashSessionRequest
        {
            OpenedAt = new DateTime(2026, 6, 15, 7, 0, 0, DateTimeKind.Utc),
            OpeningCashAmount = 100_000
        }, cashierId);

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Kasir masih memiliki sesi kasir aktif.");
    }
}
