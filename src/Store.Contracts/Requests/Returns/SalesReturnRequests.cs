namespace Store.Contracts.Requests.Returns;
public class CreateSalesReturnRequest { public Guid SaleId { get; set; } public string? Reason { get; set; } public List<ReturnItemRequest> Items { get; set; } = new(); }
public class ReturnItemRequest { public Guid ProductId { get; set; } public int Quantity { get; set; } }
