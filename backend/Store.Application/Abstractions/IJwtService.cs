namespace Store.Application.Abstractions;

public interface IJwtService
{
    string GenerateAccessToken(Domain.Entities.User user);
    string GenerateRefreshToken();
    Guid? ValidateToken(string token);
}
