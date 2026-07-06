using System;

namespace Store.Contracts.AiChat;

public class AiChatMessageDto
{
    public string Role { get; set; } = string.Empty; // user, assistant, system, tool
    public string Message { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? FunctionName { get; set; }
    public DateTime CreatedAt { get; set; }
}
