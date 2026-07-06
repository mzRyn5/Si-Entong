using Store.Domain.Common;
namespace Store.Domain.Entities;
public class Unit : SoftDeletableEntity, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid StoreId { get; set; }
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
