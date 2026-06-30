using Store.Domain.Enums;

namespace Store.Contracts.Requests.Sales;

public class CreateSaleRequest
{
    public DateTime SaleDate { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string? Notes { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public DateTime? DueDate { get; set; }
    public List<CreateSaleItemRequest> Items { get; set; } = new();
}

public class CreateSaleItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}
