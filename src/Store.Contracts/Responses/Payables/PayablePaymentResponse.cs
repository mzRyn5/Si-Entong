namespace Store.Contracts.Responses.Payables;

public class PayablePaymentResponse
{
    public Guid Id { get; set; }
    public Guid PayableId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
