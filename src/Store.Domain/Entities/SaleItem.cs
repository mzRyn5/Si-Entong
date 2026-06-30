using Store.Domain.Common;

namespace Store.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }

    // Snapshot at time of sale (for HPP and reporting)
    public string ProductSnapshotName { get; set; } = string.Empty;
    public string ProductSnapshotSku { get; set; } = string.Empty;
    public decimal PurchasePriceSnapshot { get; set; } // For HPP calculation

    // Navigation properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
