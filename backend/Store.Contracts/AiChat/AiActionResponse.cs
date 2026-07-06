using System;

namespace Store.Contracts.AiChat;

public class AiActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
}
