using Store.Domain.Common;

namespace Store.Domain.Entities;

public class Product : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual Unit Unit { get; set; } = null!;
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // Helper properties
    public bool IsLowStock => CurrentStock > 0 && CurrentStock <= LowStockThreshold;
    public bool IsOutOfStock => CurrentStock <= 0;
    public decimal StockValue => CurrentStock * PurchasePrice;
}
