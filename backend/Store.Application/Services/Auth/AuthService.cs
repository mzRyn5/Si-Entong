using Store.Application.Abstractions;
using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Auth;
using Store.Contracts.Responses.Auth;
using Store.Contracts.Responses.Users;
using Store.Domain.Entities;

namespace Store.Application.Services.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role.ToString().ToLower(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                StoreId = user.StoreId
            }
        };
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Simplified implementation for MVP
        // In production, validate refresh token from database
        return await Task.FromResult<RefreshTokenResponse?>(null);
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Simplified implementation for MVP
        // In production, invalidate refresh token in database
        return await Task.FromResult(true);
    }

    public async Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username,
            Role = user.Role.ToString().ToLower(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            StoreId = user.StoreId
        };
    }
}
