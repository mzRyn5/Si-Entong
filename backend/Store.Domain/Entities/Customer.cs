using Store.Domain.Common;
namespace Store.Domain.Entities;
public class Customer : SoftDeletableEntity, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid StoreId { get; set; }
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
