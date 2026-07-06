namespace Store.Infrastructure.Services.Gemini;

public sealed class GeminiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.5-flash";
    public bool RequireLive { get; init; } = true;
    public bool AllowMockFallback { get; init; } = false;
    public int TimeoutSeconds { get; init; } = 15;

    public bool HasLiveApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !ApiKey.StartsWith("mock", StringComparison.OrdinalIgnoreCase);
}
