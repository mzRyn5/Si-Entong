using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class SalesReturnRepository : ISalesReturnRepository
{
    private readonly AppDbContext _context;

    public SalesReturnRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SalesReturns
            .Include(sr => sr.Sale)
            .Include(sr => sr.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SalesReturn>> GetAllAsync(
        Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SalesReturns
            .Include(sr => sr.Sale)
            .AsQueryable();

        if (saleId.HasValue)
        {
            query = query.Where(sr => sr.SaleId == saleId.Value);
        }

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(sr => sr.ReturnDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(sr => sr.ReturnDate <= toDto);
        }

        // Note: SalesReturn does not have a Status property on the entity (based on SalesReturn.cs).
        // If a status filter is requested, we can optionally skip it or log/match.

        return await query
            .OrderByDescending(sr => sr.ReturnDate)
            .ThenByDescending(sr => sr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.SalesReturns.AsQueryable();

        if (saleId.HasValue)
        {
            query = query.Where(sr => sr.SaleId == saleId.Value);
        }

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(sr => sr.ReturnDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(sr => sr.ReturnDate <= toDto);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<SalesReturn> AddAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default)
    {
        _context.SalesReturns.Add(salesReturn);
        await _context.SaveChangesAsync(cancellationToken);
        return salesReturn;
    }

    public async Task UpdateAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default)
    {
        salesReturn.UpdatedAt = DateTime.UtcNow;
        _context.SalesReturns.Update(salesReturn);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"SRT-{today:yyyyMMdd}-";

        var lastNumber = await _context.SalesReturns
            .IgnoreQueryFilters()
            .Where(sr => sr.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(sr => sr.ReturnNumber)
            .Select(sr => sr.ReturnNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
