using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Infrastructure.Persistence.Repositories;

public class AiChatRepository : IAiChatRepository
{
    private readonly AppDbContext _context;

    public AiChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AiChatSession> GetOrCreateSessionAsync(Guid sessionId, Guid userId, Guid storeId, CancellationToken cancellationToken = default)
    {
        var session = await _context.AiChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session != null && (session.UserId != userId || session.StoreId != storeId))
        {
            throw new ForbiddenException("Sesi AI tidak ditemukan atau bukan milik user ini.");
        }

        if (session == null)
        {
            session = new AiChatSession
            {
                Id = sessionId,
                UserId = userId,
                StoreId = storeId,
                LastActiveAt = DateTime.UtcNow,
                Status = "active"
            };
            await _context.AiChatSessions.AddAsync(session, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            session.LastActiveAt = DateTime.UtcNow;
            _context.AiChatSessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return session;
    }

    public async Task<AiChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task SaveMessageAsync(Guid sessionId, string role, string message, string? intent = null, string? toolCalls = null, string? toolResults = null, string? functionName = null, CancellationToken cancellationToken = default)
    {
        var chatMessage = new AiChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Message = message,
            Intent = intent,
            ToolCalls = toolCalls,
            ToolResults = toolResults,
            FunctionName = functionName,
            TokenCount = message.Length / 4
        };
        await _context.AiChatMessages.AddAsync(chatMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AiChatMessage>> GetRecentMessagesAsync(Guid sessionId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiActionDraft?> GetActiveDraftAsync(Guid sessionId, string actionName, CancellationToken cancellationToken = default)
    {
        return await _context.AiActionDrafts
            .Where(d => d.SessionId == sessionId && d.ActionName == actionName && d.Status == "pending" && d.ExpiredAt > DateTime.UtcNow)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AiActionDraft?> GetDraftByIdAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        return await _context.AiActionDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);
    }

    public async Task<AiActionDraft?> GetDraftForUserAsync(Guid draftId, Guid sessionId, Guid userId, Guid storeId, CancellationToken cancellationToken = default)
    {
        return await _context.AiActionDrafts
            .Include(d => d.Session)
            .FirstOrDefaultAsync(d =>
                d.Id == draftId
                && d.SessionId == sessionId
                && d.UserId == userId
                && d.Session.StoreId == storeId,
                cancellationToken);
    }

    public async Task<AiActionDraft?> GetLatestActiveDraftAsync(Guid sessionId, Guid userId, Guid storeId, IReadOnlyCollection<string> actionNames, CancellationToken cancellationToken = default)
    {
        return await _context.AiActionDrafts
            .Include(d => d.Session)
            .Where(d =>
                d.SessionId == sessionId
                && d.UserId == userId
                && d.Session.StoreId == storeId
                && actionNames.Contains(d.ActionName)
                && d.Status == "pending"
                && d.ExpiredAt > DateTime.UtcNow)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveDraftAsync(AiActionDraft draft, CancellationToken cancellationToken = default)
    {
        var existingDrafts = await _context.AiActionDrafts
            .Where(d => d.SessionId == draft.SessionId && d.ActionName == draft.ActionName && d.Status == "pending")
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDrafts)
        {
            existing.Status = "cancelled";
            _context.AiActionDrafts.Update(existing);

            var log = new AiActionLog
            {
                SessionId = draft.SessionId,
                UserId = draft.UserId,
                StoreId = draft.Session?.StoreId ?? Guid.Empty,
                ActionName = existing.ActionName + "_cancelled",
                RequestPayload = existing.DraftPayload,
                ResponsePayload = "{\"message\":\"Auto-cancelled by concurrent draft policy\"}",
                Status = "success"
            };
            await _context.AiActionLogs.AddAsync(log, cancellationToken);
        }

        await _context.AiActionDrafts.AddAsync(draft, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDraftStatusAsync(Guid draftId, string status, CancellationToken cancellationToken = default)
    {
        var draft = await _context.AiActionDrafts.FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);
        if (draft != null)
        {
            draft.Status = status;
            _context.AiActionDrafts.Update(draft);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SaveActionLogAsync(AiActionLog log, CancellationToken cancellationToken = default)
    {
        await _context.AiActionLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _context.AiChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session != null)
        {
            session.Status = "closed";
            _context.AiChatSessions.Update(session);

            var pendingDrafts = await _context.AiActionDrafts
                .Where(d => d.SessionId == sessionId && d.Status == "pending")
                .ToListAsync(cancellationToken);

            foreach (var draft in pendingDrafts)
            {
                draft.Status = "cancelled";
                _context.AiActionDrafts.Update(draft);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
