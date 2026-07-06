using System;
using System.Security;
using System.Text.RegularExpressions;

namespace Store.Application.Services.AiChat;

public sealed class AiInputGuard
{
    private static readonly string[] PromptInjectionPatterns =
    {
        "ignore previous",
        "system prompt",
        "bypass security",
        "became developer",
        "kamu adalah developer",
        "you are now a",
        "instructions above",
        "jailbreak",
        "ignore the instructions",
        "forget your instructions",
        "lupakan instruksi",
        "abaikan instruksi"
    };

    public string CleanOrThrow(string message)
    {
        var clean = Regex.Replace(message ?? string.Empty, "<.*?>", string.Empty).Trim();
        if (clean.Length > 1000) clean = clean[..1000];
        if (string.IsNullOrWhiteSpace(clean)) return string.Empty;

        foreach (var pattern in PromptInjectionPatterns)
        {
            if (clean.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Pesan Anda mengandung pola instruksi ilegal.");
            }
        }

        return clean;
    }
}
