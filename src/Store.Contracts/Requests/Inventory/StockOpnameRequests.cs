namespace Store.Contracts.Requests.Inventory;
public class CreateStockOpnameRequest { public string? Notes { get; set; } public List<StockOpnameItemRequest> Items { get; set; } = new(); }
public class UpdateStockOpnameRequest : CreateStockOpnameRequest { }
public class StockOpnameItemRequest { public Guid ProductId { get; set; } public int ActualStock { get; set; } public string? Notes { get; set; } }
