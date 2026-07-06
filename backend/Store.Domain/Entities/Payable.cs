using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class Payable : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string PayableNumber { get; set; } = string.Empty;
    public Guid PurchaseId { get; set; }
    public Guid SupplierId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Purchase Purchase { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PayablePayment> Payments { get; set; } = new List<PayablePayment>();
}
