using System;

namespace Store.Application.Abstractions;

public interface ITenantProvider
{
    Guid? TenantId { get; }
    string? Role { get; }
    bool IsSysAdmin { get; }
}
