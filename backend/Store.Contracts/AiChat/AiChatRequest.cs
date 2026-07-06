using System;
using System.Collections.Generic;

namespace Store.Contracts.AiChat;

public class AiChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CurrentRoute { get; set; } = string.Empty;
    public string? ActiveFormKey { get; set; }
    public Dictionary<string, object>? ActiveFormData { get; set; }
}
