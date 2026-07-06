using System.Collections.Generic;

namespace Store.Infrastructure.Services.Gemini;

public static class GeminiTools
{
    public static object GetSafetySettings()
    {
        return new object[]
        {
            new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_LOW_AND_ABOVE" },
            new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_LOW_AND_ABOVE" },
            new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_LOW_AND_ABOVE" },
            new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_LOW_AND_ABOVE" }
        };
    }
}
