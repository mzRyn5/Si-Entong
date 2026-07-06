using System.Security.Claims;
using Store.Application.Abstractions;
using Store.Domain.Entities;

namespace Store.Infrastructure.Authentication.Abstractions;

public interface IJwtService : Application.Abstractions.IJwtService
{
    ClaimsPrincipal? ValidateTokenWithClaims(string token);
}
