using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Store.Contracts.AiChat;

namespace Store.Application.Abstractions.Services;

public interface IGeminiClient
{
    Task<GeminiResult> GenerateContentWithToolsAsync(List<AiChatMessageDto> history, string currentRoute, string? activeFormKey, StoreContextSnapshot? context = null, CancellationToken cancellationToken = default);
}

public class GeminiResult
{
    public bool HasFunctionCall { get; set; }
    public string? FunctionName { get; set; }
    public string? Arguments { get; set; } // JSON string of arguments
    public string Text { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public string? FallbackReason { get; set; }
}
