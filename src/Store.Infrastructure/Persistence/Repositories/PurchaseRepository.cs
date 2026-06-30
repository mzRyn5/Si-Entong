using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Infrastructure.Persistence;

namespace Store.Infrastructure.Persistence.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;

    public PurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Purchase?> GetByNumberAsync(string purchaseNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.PurchaseNumber == purchaseNumber, cancellationToken);
    }

    public async Task<IEnumerable<Purchase>> GetAllAsync(
        Guid? supplierId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Purchases
            .Include(p => p.Supplier)
            .AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<Domain.Enums.TransactionStatus>(status, true, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }
        }

        return await query
            .OrderByDescending(p => p.PurchaseDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        Guid? supplierId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Purchases.AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.Enums.TransactionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Purchase> AddAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync(cancellationToken);
        return purchase;
    }

    public async Task UpdateAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        purchase.UpdatedAt = DateTime.UtcNow;
        _context.Purchases.Update(purchase);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        _context.Purchases.Remove(purchase);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddItemAsync(PurchaseItem item, CancellationToken cancellationToken = default)
    {
        _context.PurchaseItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GeneratePurchaseNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PUR-{today:yyyyMMdd}-";

        var lastNumber = await _context.Purchases
            .IgnoreQueryFilters()
            .Where(p => p.PurchaseNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PurchaseNumber)
            .Select(p => p.PurchaseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
