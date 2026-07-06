namespace Store.Contracts.AiChat;

public class AiQuickAction
{
    public string Label { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object? Payload { get; set; }
}
