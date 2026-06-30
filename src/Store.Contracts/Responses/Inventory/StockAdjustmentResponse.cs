namespace Store.Contracts.Responses.Inventory;

public class StockAdjustmentResponse
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<StockAdjustmentItemResponse> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class StockAdjustmentItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class StockAdjustmentListItemResponse
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
}

public class CreateStockAdjustmentResponse
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
