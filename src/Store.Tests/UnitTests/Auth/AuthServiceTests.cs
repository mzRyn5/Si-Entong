using FluentAssertions;
using Moq;
using Store.Application.Abstractions;
using Store.Application.Abstractions.Repositories;
using Store.Application.Services.Auth;
using Store.Contracts.Requests.Auth;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Xunit;

namespace Store.Tests.UnitTests.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsActiveUserSession()
    {
        var storeId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "System Administrator",
            Username = "sysadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SysAdmin123!"),
            Role = UserRole.SysAdmin,
            IsActive = true,
            CreatedAt = new DateTime(2026, 6, 29, 7, 0, 0, DateTimeKind.Utc),
            StoreId = storeId
        };

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByUsernameAsync("sysAdmin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(user)).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var service = new AuthService(userRepository.Object, jwtService.Object);

        var result = await service.LoginAsync(new LoginRequest
        {
            Username = "sysAdmin",
            Password = "SysAdmin123!"
        });

        result.Should().NotBeNull();
        result!.User.IsActive.Should().BeTrue();
        result.User.StoreId.Should().Be(storeId);
        result.User.CreatedAt.Should().Be(user.CreatedAt);
    }
}
