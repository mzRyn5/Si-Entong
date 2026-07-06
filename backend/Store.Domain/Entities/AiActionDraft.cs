using System;
using Store.Domain.Common;

namespace Store.Domain.Entities;

public class AiActionDraft : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string ActionName { get; set; } = string.Empty; // create_sale, update_price, etc.
    public string EntityType { get; set; } = string.Empty;  // sale, product, customer, supplier
    public string DraftPayload { get; set; } = string.Empty; // JSON string of data
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, committed, cancelled, expired
    public DateTime ExpiredAt { get; set; }

    public AiChatSession Session { get; set; } = null!;
}
