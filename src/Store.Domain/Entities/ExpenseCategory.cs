using Store.Domain.Common;

namespace Store.Domain.Entities;

public class ExpenseCategory : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
