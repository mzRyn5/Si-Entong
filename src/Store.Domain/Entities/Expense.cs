using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class Expense : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }

    // Navigation properties
    public virtual ExpenseCategory Category { get; set; } = null!;
}
