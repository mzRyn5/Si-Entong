using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Store.Contracts.AiChat;

namespace Store.Application.Services.AiChat;

public interface IAiChatService
{
    Task<AiChatResponse> HandleMessageAsync(AiChatRequest request, Guid userId, Guid storeId);
    Task<AiActionResponse> ExecuteActionAsync(AiActionRequest request, Guid userId, Guid storeId);
    Task<List<AiChatMessageDto>> GetSessionHistoryAsync(Guid sessionId, Guid userId);
    Task CloseSessionAsync(Guid sessionId, Guid userId);
}
