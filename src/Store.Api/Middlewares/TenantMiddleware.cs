using System.Security.Claims;
using Store.Application.Abstractions;

namespace Store.Api.Middlewares;

/// <summary>
/// Middleware to set tenant context for each request.
/// SysAdmin users bypass tenant filtering (they have no StoreId).
/// For non-SysAdmin users, TenantId is extracted from the JWT "store_id" claim.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Simply pass through — tenant context is already resolved by TenantProvider
        // via IHttpContextAccessor on each request. No blocking logic needed here.
        await _next(context);
    }
}
