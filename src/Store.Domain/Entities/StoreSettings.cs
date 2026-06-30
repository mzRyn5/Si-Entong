using Store.Domain.Common;

namespace Store.Domain.Entities;

public class StoreSettings : AuditableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public bool AllowNegativeStock { get; set; } = false;
    public bool RequireCashSessionForSales { get; set; } = true;
    public int DefaultLowStockThreshold { get; set; } = 5;
    public bool EnableBarcode { get; set; } = false;
    public bool EnablePurchasePriceTracking { get; set; } = true;
    public string DefaultPaymentMethod { get; set; } = "Cash";
}
