using System;
using System.Collections.Generic;

namespace Store.Contracts.Responses.Returns;

public class PurchaseReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTimeOffset ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public List<PurchaseReturnItemResponse> Items { get; set; } = new();
}

public class PurchaseReturnItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; } // for frontend mapping compatibility
    public decimal TotalPrice { get; set; }
}
