using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Store.Domain.Entities;

namespace Store.Application.Abstractions.Repositories;

public interface IAiChatRepository
{
    Task<AiChatSession> GetOrCreateSessionAsync(Guid sessionId, Guid userId, Guid storeId, CancellationToken cancellationToken = default);
    Task<AiChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SaveMessageAsync(Guid sessionId, string role, string message, string? intent = null, string? toolCalls = null, string? toolResults = null, string? functionName = null, CancellationToken cancellationToken = default);
    Task<List<AiChatMessage>> GetRecentMessagesAsync(Guid sessionId, int limit, CancellationToken cancellationToken = default);
    Task<AiActionDraft?> GetActiveDraftAsync(Guid sessionId, string actionName, CancellationToken cancellationToken = default);
    Task<AiActionDraft?> GetDraftByIdAsync(Guid draftId, CancellationToken cancellationToken = default);
    Task<AiActionDraft?> GetDraftForUserAsync(Guid draftId, Guid sessionId, Guid userId, Guid storeId, CancellationToken cancellationToken = default);
    Task<AiActionDraft?> GetLatestActiveDraftAsync(Guid sessionId, Guid userId, Guid storeId, IReadOnlyCollection<string> actionNames, CancellationToken cancellationToken = default);
    Task SaveDraftAsync(AiActionDraft draft, CancellationToken cancellationToken = default);
    Task UpdateDraftStatusAsync(Guid draftId, string status, CancellationToken cancellationToken = default);
    Task SaveActionLogAsync(AiActionLog log, CancellationToken cancellationToken = default);
    Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
}
