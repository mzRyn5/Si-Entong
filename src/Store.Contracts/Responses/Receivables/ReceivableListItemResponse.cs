namespace Store.Contracts.Responses.Receivables;

public class ReceivableListItemResponse
{
    public Guid Id { get; set; }
    public string ReceivableNumber { get; set; } = string.Empty;
    public string SaleNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
