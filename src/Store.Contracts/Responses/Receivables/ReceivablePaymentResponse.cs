namespace Store.Contracts.Responses.Receivables;

public class ReceivablePaymentResponse
{
    public Guid Id { get; set; }
    public Guid ReceivableId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
