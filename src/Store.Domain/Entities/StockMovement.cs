using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime MovementDate { get; set; }
    public StockMovementType MovementType { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}
