namespace Store.Contracts.Responses.Expenses;

public class ExpenseResponse
{
    public Guid Id { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ExpenseNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
}

