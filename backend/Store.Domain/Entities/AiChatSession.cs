using System;
using System.Collections.Generic;
using Store.Domain.Common;

namespace Store.Domain.Entities;

public class AiChatSession : BaseEntity, ITenantEntity
{
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public DateTime LastActiveAt { get; set; }
    public string Status { get; set; } = "active"; // active, closed, expired

    public ICollection<AiChatMessage> Messages { get; set; } = new List<AiChatMessage>();
    public ICollection<AiActionDraft> ActionDrafts { get; set; } = new List<AiActionDraft>();

    public AiChatSession() : base()
    {
        LastActiveAt = DateTime.UtcNow;
    }
}
