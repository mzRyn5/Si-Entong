using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Api.Controllers;
using Xunit;

namespace Store.Tests.IntegrationTests;

public class AuthorizationPolicyTests
{
    [Fact]
    public void NonAuthControllers_RequireAuthorizationAtControllerLevel()
    {
        var controllerTypes = typeof(StoreController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && type != typeof(AuthController)
                && type != typeof(BaseApiController))
            .ToList();

        controllerTypes.Should().NotBeEmpty();
        controllerTypes.Should().OnlyContain(type =>
            type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(),
            "all non-auth API controllers must require an authenticated user");
    }

    [Theory]
    [InlineData(typeof(UsersController), nameof(UsersController.Create), "SysAdminOrOwner")]
    [InlineData(typeof(UsersController), nameof(UsersController.Update), "SysAdminOrOwner")]
    [InlineData(typeof(UsersController), nameof(UsersController.ResetPassword), "SysAdminOrOwner")]
    [InlineData(typeof(StoreController), nameof(StoreController.UpdateProfile), "OwnerOnly")]
    [InlineData(typeof(StoreController), nameof(StoreController.UpdateSettings), "OwnerOnly")]
    public void SensitiveActions_RequireExpectedAuthorizationPolicy(
        Type controllerType,
        string actionName,
        string expectedPolicy)
    {
        var action = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(method => method.Name == actionName);

        action.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should()
            .Contain(attribute => attribute.Policy == expectedPolicy);
    }
}
