using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Store.Application.Abstractions;

namespace Store.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var storeIdClaim = user.FindFirst("store_id")?.Value
                               ?? user.FindFirst("StoreId")?.Value
                               ?? user.FindFirst(c => c.Type.EndsWith("store_id", StringComparison.OrdinalIgnoreCase))?.Value;

            if (Guid.TryParse(storeIdClaim, out var storeId))
            {
                return storeId;
            }

            return null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value 
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

    public bool IsSysAdmin => string.Equals(Role, "sysadmin", StringComparison.OrdinalIgnoreCase);
}
