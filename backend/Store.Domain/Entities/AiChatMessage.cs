using System;
using Store.Domain.Common;

namespace Store.Domain.Entities;

public class AiChatMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // user, assistant, system, tool
    public string Message { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? ToolCalls { get; set; }    // JSON string for function calling payload
    public string? ToolResults { get; set; }  // JSON string for function call result
    public string? FunctionName { get; set; }
    public int TokenCount { get; set; }

    public AiChatSession Session { get; set; } = null!;
}
