namespace Store.Contracts.Requests.Purchases;

public class CreatePurchaseRequest
{
    public DateTimeOffset PurchaseDate { get; set; }
    public Guid SupplierId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
    public DateTimeOffset? DueDate { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal AmountPaid { get; set; } = 0;
    public string? Notes { get; set; }
    public List<CreatePurchaseItemRequest> Items { get; set; } = new();
}

public class CreatePurchaseItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
