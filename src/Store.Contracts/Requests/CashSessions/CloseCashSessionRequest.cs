namespace Store.Contracts.Requests.CashSessions;

public class CloseCashSessionRequest
{
    public DateTime ClosedAt { get; set; }
    public decimal ActualCashAmount { get; set; }
    public string? Notes { get; set; }
}
