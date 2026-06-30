using Store.Domain.Common;
namespace Store.Domain.Entities;
public class StockOpnameItem : BaseEntity { public Guid StockOpnameId { get; set; } public Guid ProductId { get; set; } public int SystemStock { get; set; } public int ActualStock { get; set; } public int Difference { get; set; } public string? Notes { get; set; } public virtual StockOpname StockOpname { get; set; } = null!; public virtual Product Product { get; set; } = null!; }
