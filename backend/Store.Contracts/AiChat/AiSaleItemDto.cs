using System;

namespace Store.Contracts.AiChat;

public class AiSaleItemDto
{
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; } // Nullable: jika null, backend isi dari database
}
