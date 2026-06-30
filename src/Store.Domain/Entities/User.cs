using Store.Domain.Common;
using Store.Domain.Enums;

namespace Store.Domain.Entities;

public class User : SoftDeletableEntity, IOptionalTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? StoreId { get; set; }
}
