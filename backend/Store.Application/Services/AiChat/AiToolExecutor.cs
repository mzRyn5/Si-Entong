using System;
using System.Text.Json;
using System.Threading.Tasks;
using Store.Application.Services.AiChat.Tools;

namespace Store.Application.Services.AiChat;

public class AiToolExecutor : IAiToolExecutor
{
    private readonly AiToolRegistry _registry;

    public AiToolExecutor(AiToolRegistry registry)
    {
        _registry = registry;
    }

    public async Task<object> ExecuteAsync(Guid sessionId, string functionName, string argumentsJson, Guid userId, Guid storeId)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var context = new AiToolExecutionContext(sessionId, userId, storeId, functionName, doc.RootElement.Clone());
            return await _registry.Resolve(functionName).ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            return new { error = $"Gagal mengeksekusi aksi: {ex.Message}" };
        }
    }
}
