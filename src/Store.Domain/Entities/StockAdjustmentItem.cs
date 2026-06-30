using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class StockAdjustmentItem : BaseEntity
{
    public Guid StockAdjustmentId { get; set; }
    public Guid ProductId { get; set; }
    public StockAdjustmentType AdjustmentType { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }

    public virtual StockAdjustment StockAdjustment { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
