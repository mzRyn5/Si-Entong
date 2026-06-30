using Store.Domain.Common;
namespace Store.Domain.Entities;
public class StockOpname : AuditableEntity, ITenantEntity
{
    public string OpnameNumber { get; set; } = string.Empty;
    public DateTimeOffset OpnameDate { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public Guid StoreId { get; set; }
    public virtual ICollection<StockOpnameItem> Items { get; set; } = new List<StockOpnameItem>();
}
