using System.Collections.Generic;

namespace Store.Contracts.AiChat;

public class AiChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string ResponseType { get; set; } = "text"; // text, navigation, fill_form, draft_preview, clarification_needed, error
    public string? DraftId { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool UsedFallback { get; set; }
    public string? FallbackReason { get; set; }
    public AiUiAction? UiAction { get; set; }
    public List<AiQuickAction> QuickActions { get; set; } = new();
}
