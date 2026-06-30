using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class CashMovement : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CashSessionId { get; set; }
    public DateTime MovementDate { get; set; }
    public CashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public TransactionStatus Status { get; set; }

    // Navigation properties
    public virtual CashSession CashSession { get; set; } = null!;
}
