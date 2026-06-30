namespace Store.Contracts.Responses.Inventory;

public class StockSummaryResponse
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; }
    public decimal StockValue { get; set; }
    public decimal SellingPrice { get; set; }
}

public class StockMovementResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTimeOffset MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
