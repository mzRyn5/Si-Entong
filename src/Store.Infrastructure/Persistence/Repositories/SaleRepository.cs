using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Cashier)
            .Include(s => s.Customer)
            .Include(s => s.CashSession)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Sale>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(cashierId, fromDate, toDate, status);

        return await query
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        CancellationToken cancellationToken = default)
    {
        return await BuildFilteredQuery(cashierId, fromDate, toDate, status)
            .CountAsync(cancellationToken);
    }

    public async Task<Sale> AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        sale.UpdatedAt = DateTime.UtcNow;
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateSaleNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"SAL-{today:yyyyMMdd}-";

        var lastNumber = await _context.Sales
            .IgnoreQueryFilters()
            .Where(s => s.SaleNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SaleNumber)
            .Select(s => s.SaleNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }

    private IQueryable<Sale> BuildFilteredQuery(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status)
    {
        var query = _context.Sales
            .Include(s => s.Cashier)
            .Include(s => s.Customer)
            .AsQueryable();

        if (cashierId.HasValue)
        {
            query = query.Where(s => s.CashierId == cashierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.SaleDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.SaleDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TransactionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        return query;
    }
}

public class CashSessionRepository : ICashSessionRepository
{
    private readonly AppDbContext _context;

    public CashSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CashSessions
            .Include(c => c.Cashier)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CashSession?> GetActiveByCashierIdAsync(Guid cashierId, CancellationToken cancellationToken = default)
    {
        return await _context.CashSessions
            .Include(c => c.Cashier)
            .FirstOrDefaultAsync(c => c.CashierId == cashierId && c.Status == CashSessionStatus.Open, cancellationToken);
    }

    public async Task<IEnumerable<CashSession>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(cashierId, fromDate, toDate, status);

        return await query
            .OrderByDescending(c => c.OpenedAt)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        CancellationToken cancellationToken = default)
    {
        return await BuildFilteredQuery(cashierId, fromDate, toDate, status)
            .CountAsync(cancellationToken);
    }

    public async Task<CashSession> AddAsync(CashSession cashSession, CancellationToken cancellationToken = default)
    {
        _context.CashSessions.Add(cashSession);
        await _context.SaveChangesAsync(cancellationToken);
        return cashSession;
    }

    public async Task UpdateAsync(CashSession cashSession, CancellationToken cancellationToken = default)
    {
        cashSession.UpdatedAt = DateTime.UtcNow;
        _context.CashSessions.Update(cashSession);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<CashSession> BuildFilteredQuery(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status)
    {
        var query = _context.CashSessions
            .Include(c => c.Cashier)
            .AsQueryable();

        if (cashierId.HasValue)
        {
            query = query.Where(c => c.CashierId == cashierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.OpenedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.OpenedAt <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CashSessionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        return query;
    }
}
