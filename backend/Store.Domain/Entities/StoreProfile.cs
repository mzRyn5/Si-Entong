using Store.Domain.Common;

namespace Store.Domain.Entities;

public class StoreProfile : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Currency { get; set; } = "IDR";
    public string Timezone { get; set; } = "Asia/Jakarta";
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool IsActive { get; set; } = true;
}
