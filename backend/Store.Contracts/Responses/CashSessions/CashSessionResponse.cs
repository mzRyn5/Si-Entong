namespace Store.Contracts.Responses.CashSessions;

public class CashSessionResponse
{
    public Guid Id { get; set; }
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal CashSalesAmount { get; set; }
    public decimal CashInAmount { get; set; }
    public decimal CashOutAmount { get; set; }
    public decimal ExpectedCashAmount { get; set; }
    public decimal? ActualCashAmount { get; set; }
    public decimal? DifferenceAmount { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CashSessionListItemResponse
{
    public Guid Id { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
