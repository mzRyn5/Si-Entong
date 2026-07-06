using System;

namespace Store.Contracts.AiChat;

public class StoreContextSnapshot
{
    public string StoreName { get; set; } = string.Empty;
    public string TodayDate { get; set; } = string.Empty;
    public decimal TodaySalesAmount { get; set; }
    public int TodaySalesCount { get; set; }
    public decimal TodayPurchaseAmount { get; set; }
    public decimal TodayExpenseAmount { get; set; }
    public decimal TodayGrossProfit { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public string? ActiveSaleDraftJson { get; set; }
}
