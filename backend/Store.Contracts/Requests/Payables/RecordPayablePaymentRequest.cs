namespace Store.Contracts.Requests.Payables;

public class RecordPayablePaymentRequest
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Notes { get; set; }
}
