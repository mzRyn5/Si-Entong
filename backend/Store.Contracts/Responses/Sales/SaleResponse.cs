namespace Store.Contracts.Responses.Sales;

public class SaleResponse
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public CashierResponse? Cashier { get; set; }
    public CustomerResponse? Customer { get; set; }
    public List<SaleItemResponse> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SaleItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
}

public class CashierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SaleListItemResponse
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SaleReceiptResponse
{
    public string StoreName { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public List<SaleReceiptItemResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
}

public class SaleReceiptItemResponse
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
