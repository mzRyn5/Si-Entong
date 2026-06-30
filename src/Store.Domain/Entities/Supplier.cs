using Store.Domain.Common;
namespace Store.Domain.Entities;
public class Supplier : SoftDeletableEntity, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid StoreId { get; set; }
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
