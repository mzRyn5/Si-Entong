using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Store.Contracts.Responses.Common;
using Store.Api.Extensions;

namespace Store.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }

    protected string CurrentUserRole
    {
        get
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }

    protected string CurrentUsername
    {
        get
        {
            return User.FindFirst("username")?.Value ?? string.Empty;
        }
    }

    protected bool IsSysAdmin => CurrentUserRole.Equals("sysadmin", StringComparison.OrdinalIgnoreCase);
    protected bool IsOwner => CurrentUserRole.Equals("owner", StringComparison.OrdinalIgnoreCase);
    protected bool IsAdmin => CurrentUserRole.Equals("admin", StringComparison.OrdinalIgnoreCase);
    protected bool IsOwnerOrAdmin => IsOwner || IsAdmin || IsSysAdmin;

    protected IActionResult OkResponse<T>(T data, string? message = null)
    {
        return SuccessResponse(data, message);
    }

    protected IActionResult OkResponse(string? message = null)
    {
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = message ?? "Success",
            Data = null
        };

        return Ok(response);
    }

    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Success",
            Data = data
        };

        return Ok(response);
    }

    protected IActionResult CreatedResponse<T>(T data, string? message = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Data berhasil dibuat.",
            Data = data
        };

        return StatusCode(201, response);
    }

    protected IActionResult PagedResponse<T>(PagedResponse<T> response)
    {
        return Ok(response);
    }

    protected IActionResult ErrorResponse(string message, string errorCode, int statusCode = 400)
    {
        var response = new ApiErrorResponse
        {
            Success = false,
            Message = message,
            Error = new ErrorDetails
            {
                Code = errorCode
            },
            TraceId = HttpContext.TraceIdentifier
        };

        return StatusCode(statusCode, response);
    }

    protected IActionResult NotFoundResponse(string entityName, object id)
    {
        return ErrorResponse($"{entityName} dengan ID '{id}' tidak ditemukan.", "NOT_FOUND", 404);
    }

    protected IActionResult ValidationErrorResponse(string message, List<ErrorDetail> errors)
    {
        var response = new ApiErrorResponse
        {
            Success = false,
            Message = message,
            Error = new ErrorDetails
            {
                Code = "VALIDATION_ERROR",
                Details = errors
            },
            TraceId = HttpContext.TraceIdentifier
        };

        return BadRequest(response);
    }

    protected IActionResult ForbiddenResponse(string message = "Anda tidak memiliki akses untuk melakukan aksi ini.")
    {
        return ErrorResponse(message, "FORBIDDEN", 403);
    }

    protected IActionResult UnauthorizedResponse(string message = "Token tidak valid atau sudah expired.")
    {
        return ErrorResponse(message, "UNAUTHORIZED", 401);
    }

    protected IActionResult ConflictResponse(string message, string errorCode = "CONFLICT")
    {
        return ErrorResponse(message, errorCode, 409);
    }
}
