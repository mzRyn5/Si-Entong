using Store.Domain.Common;

namespace Store.Domain.Entities;

public class AuditLog : BaseEntity, IOptionalTenantEntity
{
    public Guid UserId { get; set; }
    public Guid? StoreId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public virtual User? User { get; set; }
}
