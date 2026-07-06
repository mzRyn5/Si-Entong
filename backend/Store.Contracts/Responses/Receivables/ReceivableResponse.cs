namespace Store.Contracts.Responses.Receivables;

public class ReceivableResponse
{
    public Guid Id { get; set; }
    public string ReceivableNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ReceivablePaymentResponse> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
