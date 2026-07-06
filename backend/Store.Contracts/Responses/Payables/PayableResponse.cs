namespace Store.Contracts.Responses.Payables;

public class PayableResponse
{
    public Guid Id { get; set; }
    public string PayableNumber { get; set; } = string.Empty;
    public Guid PurchaseId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<PayablePaymentResponse> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
