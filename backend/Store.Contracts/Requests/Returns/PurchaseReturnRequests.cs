namespace Store.Contracts.Requests.Returns;
public class CreatePurchaseReturnRequest { public Guid PurchaseId { get; set; } public string? Reason { get; set; } public List<ReturnItemRequest> Items { get; set; } = new(); }
