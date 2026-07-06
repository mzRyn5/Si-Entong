using Store.Contracts.Responses.Suppliers;

namespace Store.Contracts.Responses.Purchases;

public class PurchaseResponse
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public SupplierResponse? Supplier { get; set; }
    public List<PurchaseItemResponse> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class PurchaseItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class PurchaseListItemResponse
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}
