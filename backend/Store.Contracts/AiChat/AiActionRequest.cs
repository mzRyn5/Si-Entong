using System;

namespace Store.Contracts.AiChat;

public class AiActionRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object? Payload { get; set; }
}
