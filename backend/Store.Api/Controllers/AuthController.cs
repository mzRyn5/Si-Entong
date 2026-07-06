using Store.Application.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Auth;
using Store.Contracts.Requests.Auth;
using Store.Contracts.Responses.Auth;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Users;
using Store.Infrastructure.Authentication.Abstractions;
using Store.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserService _userService;

    public AuthController(
        AppDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        IUserService userService)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Login user dengan username dan password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var username = request.Username.Trim().ToLower();
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login attempt failed: user not found or inactive for username {Username}", request.Username);
            return UnauthorizedResponse("Username atau password salah.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed: invalid password for username {Username}", request.Username);
            return UnauthorizedResponse("Username atau password salah.");
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role.ToString().ToLower()
            }
        };

        return SuccessResponse(response, "Login berhasil.");
    }

    /// <summary>
    /// Refresh access token menggunakan refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("Token refresh requested");

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || !user.IsActive || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: invalid, inactive, or expired refresh token");
            return UnauthorizedResponse("Refresh token tidak valid atau sudah expired.");
        }

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var response = new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return SuccessResponse(response, "Token berhasil diperbarui.");
    }

    /// <summary>
    /// Logout user dan invalidate token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var user = await _context.Users.FindAsync(CurrentUserId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("User {UserId} logged out", CurrentUserId);

        return SuccessResponse("Logout berhasil.");
    }

    /// <summary>
    /// Get current logged in user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (CurrentUserId == Guid.Empty)
        {
            return UnauthorizedResponse("Token tidak valid.");
        }

        var user = await _context.Users.FindAsync(CurrentUserId);

        if (user == null || !user.IsActive)
        {
            return UnauthorizedResponse("User tidak ditemukan.");
        }

        var response = new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username,
            Role = user.Role.ToString().ToLower(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return SuccessResponse(response, "Data user berhasil diambil.");
    }

    /// <summary>
    /// Mengubah password user yang sedang login
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var success = await _userService.ChangePasswordAsync(CurrentUserId, request.OldPassword, request.NewPassword, cancellationToken);
        if (!success)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Message = "Gagal mengubah password.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return SuccessResponse<object>(null!, "Password berhasil diubah.");
    }
}
