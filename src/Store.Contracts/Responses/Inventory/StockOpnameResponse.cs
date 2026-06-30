namespace Store.Contracts.Responses.Inventory;
public class StockOpnameResponse { public Guid Id { get; set; } public string OpnameNumber { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTimeOffset OpnameDate { get; set; } public List<StockOpnameItemResponse> Items { get; set; } = new(); }
public class StockOpnameItemResponse { public Guid ProductId { get; set; } public string ProductName { get; set; } = string.Empty; public int SystemStock { get; set; } public int ActualStock { get; set; } public int Difference { get; set; } }
