namespace Store.Contracts.Requests.Receivables;

public class RecordReceivablePaymentRequest
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Notes { get; set; }
}
