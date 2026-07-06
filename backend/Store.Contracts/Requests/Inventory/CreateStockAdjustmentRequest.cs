namespace Store.Contracts.Requests.Inventory;

public class CreateStockAdjustmentRequest
{
    public DateTimeOffset AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateStockAdjustmentItemRequest> Items { get; set; } = new();
}

public class CreateStockAdjustmentItemRequest
{
    public Guid ProductId { get; set; }
    public string AdjustmentType { get; set; } = "Increase"; // Increase or Decrease
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStockAdjustmentRequest
{
    public DateTimeOffset AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateStockAdjustmentItemRequest> Items { get; set; } = new();
}

public class VoidStockAdjustmentRequest
{
    public string Reason { get; set; } = string.Empty;
}
