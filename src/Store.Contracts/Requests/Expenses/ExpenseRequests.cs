namespace Store.Contracts.Requests.Expenses;
public class CreateExpenseRequest
{
    public Guid ExpenseCategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
}
public class UpdateExpenseRequest : CreateExpenseRequest { }

