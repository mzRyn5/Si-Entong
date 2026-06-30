using Store.Domain.Common;

namespace Store.Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string ProductSnapshotName { get; set; } = string.Empty;
    public string ProductSnapshotSku { get; set; } = string.Empty;

    public virtual Purchase Purchase { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
