using Store.Domain.Common;
namespace Store.Domain.Entities;
public class PurchaseReturnItem : BaseEntity { public Guid PurchaseReturnId { get; set; } public Guid ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal TotalPrice { get; set; } public virtual PurchaseReturn PurchaseReturn { get; set; } = null!; public virtual Product Product { get; set; } = null!; }
