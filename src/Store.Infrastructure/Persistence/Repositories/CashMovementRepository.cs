using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class CashMovementRepository : ICashMovementRepository
{
    private readonly AppDbContext _context;

    public CashMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CashMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CashMovements
            .FirstOrDefaultAsync(cm => cm.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CashMovement>> GetAllAsync(
        Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CashMovements.AsQueryable();

        if (cashSessionId.HasValue)
        {
            query = query.Where(cm => cm.CashSessionId == cashSessionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (Enum.TryParse<Domain.Enums.CashMovementType>(type, true, out var typeEnum))
            {
                query = query.Where(cm => cm.MovementType == typeEnum);
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(cm => cm.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(cm => cm.MovementDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(cm => cm.MovementDate)
            .ThenByDescending(cm => cm.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.CashMovements.AsQueryable();

        if (cashSessionId.HasValue)
        {
            query = query.Where(cm => cm.CashSessionId == cashSessionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (Enum.TryParse<Domain.Enums.CashMovementType>(type, true, out var typeEnum))
            {
                query = query.Where(cm => cm.MovementType == typeEnum);
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(cm => cm.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(cm => cm.MovementDate <= toDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<CashMovement> AddAsync(CashMovement movement, CancellationToken cancellationToken = default)
    {
        _context.CashMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);
        return movement;
    }
}
