using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Auth;
using Store.Application.Services.Users;
using Store.Application.Services.MasterData;
using Store.Application.Services.Store;
using Store.Contracts.Requests.Users;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Users;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SysAdminOrOwnerOrAdmin")]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAllAsync(search, role, isActive, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SysAdminOrOwnerOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("User", id);
        }
        return SuccessResponse(result, "Data user berhasil diambil.");
    }

    /// <summary>
    /// Create new user (Owner only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SysAdminOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsOwner && !IsSysAdmin)
        {
            return ForbiddenResponse();
        }

        var result = await _userService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "User berhasil dibuat.");
    }

    /// <summary>
    /// Update existing user (Owner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SysAdminOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsOwner && !IsSysAdmin)
        {
            return ForbiddenResponse();
        }

        var result = await _userService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("User", id);
        }
        return SuccessResponse(result, "User berhasil diperbarui.");
    }

    /// <summary>
    /// Reset user password (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = "SysAdminOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsOwner && !IsSysAdmin)
        {
            return ForbiddenResponse();
        }

        var result = await _userService.ResetPasswordAsync(id, request.NewPassword, CurrentUserId, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("User", id);
        }
        return SuccessResponse("Password berhasil direset.");
    }

    /// <summary>
    /// Delete user (Owner or SysAdmin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SysAdminOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsOwner && !IsSysAdmin)
        {
            return ForbiddenResponse();
        }

        var result = await _userService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFoundResponse("User", id);
        }
        return SuccessResponse("User berhasil dihapus.");
    }
}
