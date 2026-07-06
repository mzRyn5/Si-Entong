namespace Store.Contracts.Responses.Products;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public bool IsActive { get; set; }
}
