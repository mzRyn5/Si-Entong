using System;
using System.Threading.Tasks;

namespace Store.Application.Services.AiChat;

public interface IAiToolExecutor
{
    Task<object> ExecuteAsync(Guid sessionId, string functionName, string argumentsJson, Guid userId, Guid storeId);
}
