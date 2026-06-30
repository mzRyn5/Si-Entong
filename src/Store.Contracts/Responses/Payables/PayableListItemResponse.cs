namespace Store.Contracts.Responses.Payables;

public class PayableListItemResponse
{
    public Guid Id { get; set; }
    public string PayableNumber { get; set; } = string.Empty;
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
