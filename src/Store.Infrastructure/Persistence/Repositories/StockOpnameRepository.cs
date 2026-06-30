using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class StockOpnameRepository : IStockOpnameRepository
{
    private readonly AppDbContext _context;

    public StockOpnameRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockOpname?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StockOpnames
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<StockOpname>> GetAllAsync(
        DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.StockOpnames.AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(o => o.OpnameDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(o => o.OpnameDate <= toDto);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        return await query
            .OrderByDescending(o => o.OpnameDate)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.StockOpnames.AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(o => o.OpnameDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(o => o.OpnameDate <= toDto);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<StockOpname> AddAsync(StockOpname opname, CancellationToken cancellationToken = default)
    {
        _context.StockOpnames.Add(opname);
        await _context.SaveChangesAsync(cancellationToken);
        return opname;
    }

    public async Task UpdateAsync(StockOpname opname, CancellationToken cancellationToken = default)
    {
        opname.UpdatedAt = DateTime.UtcNow;
        _context.StockOpnames.Update(opname);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateOpnameNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"OPN-{today:yyyyMMdd}-";

        var lastNumber = await _context.StockOpnames
            .IgnoreQueryFilters()
            .Where(o => o.OpnameNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OpnameNumber)
            .Select(o => o.OpnameNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
