using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class CashSession : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CashierId { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal CashSalesAmount { get; set; }
    public decimal CashInAmount { get; set; }
    public decimal CashOutAmount { get; set; }
    public decimal ExpectedCashAmount => OpeningCashAmount + CashSalesAmount + CashInAmount - CashOutAmount;
    public decimal? ActualCashAmount { get; set; }
    public decimal? DifferenceAmount => ActualCashAmount.HasValue ? ActualCashAmount.Value - ExpectedCashAmount : null;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? ClosingNotes { get; set; }
    public CashSessionStatus Status { get; set; }

    // Navigation properties
    public virtual User Cashier { get; set; } = null!;
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}
