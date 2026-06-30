using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class Sale : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CashierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }

    // Navigation properties
    public virtual User Cashier { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual CashSession? CashSession { get; set; }
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
