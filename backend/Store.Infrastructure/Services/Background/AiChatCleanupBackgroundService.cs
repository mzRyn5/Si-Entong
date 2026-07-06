using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Store.Infrastructure.Persistence;

namespace Store.Infrastructure.Services.Background;

public class AiChatCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiChatCleanupBackgroundService> _logger;

    public AiChatCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AiChatCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Chat Cleanup Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing AI Chat cleanup.");
            }

            // Run every 15 minutes
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        _logger.LogInformation("AI Chat Cleanup Background Service is stopping.");
    }

    private async Task DoCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Expire pending drafts older than 10 minutes (bypassing tenant filter to clean up globally)
        var expiredDrafts = await context.AiActionDrafts
            .IgnoreQueryFilters()
            .Where(d => d.Status == "pending" && d.ExpiredAt < DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        if (expiredDrafts.Count > 0)
        {
            _logger.LogInformation("Found {Count} expired AI drafts to update.", expiredDrafts.Count);
            foreach (var draft in expiredDrafts)
            {
                draft.Status = "expired";
                context.AiActionDrafts.Update(draft);
            }
        }

        // 2. Mark sessions as expired if they have been inactive for more than 24 hours
        var inactiveSessions = await context.AiChatSessions
            .IgnoreQueryFilters()
            .Where(s => s.Status == "active" && s.LastActiveAt < DateTime.UtcNow.AddDays(-1))
            .ToListAsync(stoppingToken);

        if (inactiveSessions.Count > 0)
        {
            _logger.LogInformation("Found {Count} inactive AI chat sessions to close.", inactiveSessions.Count);
            foreach (var session in inactiveSessions)
            {
                session.Status = "expired";
                context.AiChatSessions.Update(session);
            }
        }

        if (expiredDrafts.Count > 0 || inactiveSessions.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
