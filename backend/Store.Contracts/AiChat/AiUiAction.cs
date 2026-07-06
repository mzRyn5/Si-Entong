using System.Collections.Generic;

namespace Store.Contracts.AiChat;

public class AiUiAction
{
    public string Type { get; set; } = string.Empty; // navigate, fill_form, refresh_table
    public string? Route { get; set; }
    public string? FormKey { get; set; }
    public Dictionary<string, object>? Query { get; set; }
    public Dictionary<string, object>? Fields { get; set; }
}
