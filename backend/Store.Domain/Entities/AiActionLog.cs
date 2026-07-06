using System;
using Store.Domain.Common;

namespace Store.Domain.Entities;

public class AiActionLog : BaseEntity, ITenantEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty; // JSON string
    public string ResponsePayload { get; set; } = string.Empty; // JSON string
    public string Status { get; set; } = string.Empty; // success, error
}
