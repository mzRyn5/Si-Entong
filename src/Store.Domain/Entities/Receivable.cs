using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class Receivable : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string ReceivableNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<ReceivablePayment> Payments { get; set; } = new List<ReceivablePayment>();
}
