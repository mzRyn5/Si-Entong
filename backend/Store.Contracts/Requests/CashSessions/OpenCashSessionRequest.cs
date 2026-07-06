namespace Store.Contracts.Requests.CashSessions;

public class OpenCashSessionRequest
{
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public string? Notes { get; set; }
}
