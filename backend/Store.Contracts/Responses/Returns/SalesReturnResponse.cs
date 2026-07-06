using System;
using System.Collections.Generic;

namespace Store.Contracts.Responses.Returns;

public class SalesReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTimeOffset ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalRefundAmount { get; set; } // for frontend mapping compatibility
    public string Status { get; set; } = "Completed";
    public List<SalesReturnItemResponse> Items { get; set; } = new();
}

public class SalesReturnItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; } // for frontend mapping compatibility
    public decimal TotalPrice { get; set; }
}
