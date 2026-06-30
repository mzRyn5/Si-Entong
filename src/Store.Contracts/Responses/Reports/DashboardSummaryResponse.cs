namespace Store.Contracts.Responses.Reports;

public class DashboardSummaryResponse
{
    public DateTime Date { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int TotalSalesTransactions { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalExpenseAmount { get; set; }
    public decimal GrossProfitAmount { get; set; }
    public int LowStockProductCount { get; set; }
    public int OutOfStockProductCount { get; set; }
    public List<PaymentMethodSummaryResponse> PaymentMethodSummaries { get; set; } = new();
    public List<LatestActivityResponse> LatestActivities { get; set; } = new();
}

public class PaymentMethodSummaryResponse
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class LatestActivityResponse
{
    public DateTime CreatedAt { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
