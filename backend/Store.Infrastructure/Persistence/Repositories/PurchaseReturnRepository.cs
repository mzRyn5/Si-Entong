using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class PurchaseReturnRepository : IPurchaseReturnRepository
{
    private readonly AppDbContext _context;

    public PurchaseReturnRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseReturns
            .Include(pr => pr.Purchase)
            .Include(pr => pr.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PurchaseReturn>> GetAllAsync(
        Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseReturns
            .Include(pr => pr.Purchase)
            .AsQueryable();

        if (purchaseId.HasValue)
        {
            query = query.Where(pr => pr.PurchaseId == purchaseId.Value);
        }

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(pr => pr.ReturnDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(pr => pr.ReturnDate <= toDto);
        }

        return await query
            .OrderByDescending(pr => pr.ReturnDate)
            .ThenByDescending(pr => pr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseReturns.AsQueryable();

        if (purchaseId.HasValue)
        {
            query = query.Where(pr => pr.PurchaseId == purchaseId.Value);
        }

        if (fromDate.HasValue)
        {
            var fromDto = new DateTimeOffset(fromDate.Value);
            query = query.Where(pr => pr.ReturnDate >= fromDto);
        }

        if (toDate.HasValue)
        {
            var toDto = new DateTimeOffset(toDate.Value);
            query = query.Where(pr => pr.ReturnDate <= toDto);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<PurchaseReturn> AddAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default)
    {
        _context.PurchaseReturns.Add(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
        return purchaseReturn;
    }

    public async Task UpdateAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default)
    {
        purchaseReturn.UpdatedAt = DateTime.UtcNow;
        _context.PurchaseReturns.Update(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PRT-{today:yyyyMMdd}-";

        var lastNumber = await _context.PurchaseReturns
            .IgnoreQueryFilters()
            .Where(pr => pr.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(pr => pr.ReturnNumber)
            .Select(pr => pr.ReturnNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
