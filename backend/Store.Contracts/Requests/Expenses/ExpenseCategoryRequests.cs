namespace Store.Contracts.Requests.Expenses;
public class CreateExpenseCategoryRequest { public string Name { get; set; } = string.Empty; public string? Description { get; set; } }
public class UpdateExpenseCategoryRequest : CreateExpenseCategoryRequest { }
