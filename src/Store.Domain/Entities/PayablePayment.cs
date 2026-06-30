using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class PayablePayment : AuditableEntity
{
    public Guid PayableId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Payable Payable { get; set; } = null!;
}
