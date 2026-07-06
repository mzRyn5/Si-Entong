using Store.Domain.Common;
namespace Store.Domain.Entities;
public class SalesReturn : AuditableEntity, ITenantEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public DateTimeOffset ReturnDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid StoreId { get; set; }
    public virtual Sale Sale { get; set; } = null!;
    public virtual ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}
