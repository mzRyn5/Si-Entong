using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Infrastructure.Persistence;

namespace Store.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StockMovement>> GetAllAsync(
        Guid? productId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockMovements
            .Include(s => s.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(s => s.ProductId == productId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.MovementDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(s => s.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        Guid? productId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockMovements.AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(s => s.ProductId == productId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.MovementDate <= toDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(
        Guid? userId, string? action, string? module,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .AsQueryable();

        var tenantId = _context.TenantProviderInstance?.TenantId;
        var isSysAdmin = _context.TenantProviderInstance?.IsSysAdmin ?? false;

        if (!isSysAdmin && tenantId.HasValue && tenantId != Guid.Empty)
        {
            query = query.Where(a => a.StoreId == tenantId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.Module.Contains(module));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? userId, string? action, string? module, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .IgnoreQueryFilters()
            .AsQueryable();

        var tenantId = _context.TenantProviderInstance?.TenantId;
        var isSysAdmin = _context.TenantProviderInstance?.IsSysAdmin ?? false;

        if (!isSysAdmin && tenantId.HasValue && tenantId != Guid.Empty)
        {
            query = query.Where(a => a.StoreId == tenantId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.Module.Contains(module));
        }

        return await query.CountAsync(cancellationToken);
    }
}

public class StoreProfileRepository : IStoreProfileRepository
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public StoreProfileRepository(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<StoreProfile?> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            return await _context.StoreProfiles.FirstOrDefaultAsync(s => s.Id == tenantId.Value, cancellationToken);
        }
        return await _context.StoreProfiles.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StoreProfile> AddAsync(StoreProfile profile, CancellationToken cancellationToken = default)
    {
        _context.StoreProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task UpdateAsync(StoreProfile profile, CancellationToken cancellationToken = default)
    {
        _context.StoreProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class StoreSettingsRepository : IStoreSettingsRepository
{
    private readonly AppDbContext _context;

    public StoreSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StoreSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StoreSettings.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StoreSettings> AddAsync(StoreSettings settings, CancellationToken cancellationToken = default)
    {
        _context.StoreSettings.Add(settings);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task UpdateAsync(StoreSettings settings, CancellationToken cancellationToken = default)
    {
        _context.StoreSettings.Update(settings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly AppDbContext _context;

    public StockAdjustmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockAdjustment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StockAdjustments
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<StockAdjustment?> GetByNumberAsync(string adjustmentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.StockAdjustments
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.AdjustmentNumber == adjustmentNumber, cancellationToken);
    }

    public async Task<IEnumerable<StockAdjustment>> GetAllAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments
            .Include(s => s.Items)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.AdjustmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.AdjustmentDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<Domain.Enums.TransactionStatus>(status, true, out var statusEnum))
            {
                query = query.Where(s => s.Status == statusEnum);
            }
        }

        return await query
            .OrderByDescending(s => s.AdjustmentDate)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.AdjustmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.AdjustmentDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.Enums.TransactionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<StockAdjustment> AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        _context.StockAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);
        return adjustment;
    }

    public async Task UpdateAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        adjustment.UpdatedAt = DateTime.UtcNow;
        _context.StockAdjustments.Update(adjustment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateAdjustmentNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"ADJ-{today:yyyyMMdd}-";

        var lastNumber = await _context.StockAdjustments
            .IgnoreQueryFilters()
            .Where(s => s.AdjustmentNumber.StartsWith(prefix))
            .OrderByDescending(s => s.AdjustmentNumber)
            .Select(s => s.AdjustmentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
