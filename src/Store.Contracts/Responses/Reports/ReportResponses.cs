namespace Store.Contracts.Responses.Reports;

public class DailySalesReportResponse
{
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal NetSalesAmount { get; set; }
}

public class ProductSalesReportResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal GrossSalesAmount { get; set; }
    public decimal EstimatedCostAmount { get; set; }
    public decimal EstimatedGrossProfitAmount { get; set; }
}

public class StockValuationReportResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal StockValue { get; set; }
}

public class BasicProfitReportResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal NetSalesAmount { get; set; }
    public decimal EstimatedCostOfGoodsSold { get; set; }
    public decimal EstimatedGrossProfit { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal EstimatedNetProfit { get; set; }
}

public class PurchaseReportResponse
{
    public Guid PurchaseId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal GrossPurchaseAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal NetPurchaseAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class ExpenseReportResponse
{
    public Guid ExpenseId { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class InventoryMovementReportResponse
{
    public Guid MovementId { get; set; }
    public DateTime MovementDate { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}
