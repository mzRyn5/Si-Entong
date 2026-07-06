using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class Purchase : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public Guid SupplierId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
