using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class StockAdjustment : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Draft;
    public string? VoidReason { get; set; }
    public Guid? VoidedBy { get; set; }
    public DateTime? VoidedAt { get; set; }

    // Navigation properties
    public virtual ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
}
