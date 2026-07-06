using Store.Domain.Common;
namespace Store.Domain.Entities;
public class PurchaseReturn : AuditableEntity, ITenantEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseId { get; set; }
    public DateTimeOffset ReturnDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid StoreId { get; set; }
    public virtual Purchase Purchase { get; set; } = null!;
    public virtual ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}
