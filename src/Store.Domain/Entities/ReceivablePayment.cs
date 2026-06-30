using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class ReceivablePayment : AuditableEntity
{
    public Guid ReceivableId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Receivable Receivable { get; set; } = null!;
}
