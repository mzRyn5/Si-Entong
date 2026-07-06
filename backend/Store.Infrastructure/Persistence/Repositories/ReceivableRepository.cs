using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Repositories;

public class ReceivableRepository : IReceivableRepository
{
    private readonly AppDbContext _context;

    public ReceivableRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Receivable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Receivables
            .Include(r => r.Customer)
            .Include(r => r.Sale)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Receivable>> GetAllAsync(Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Receivables.Include(r => r.Customer).Include(r => r.Sale).AsNoTracking().AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                query = query.Where(r => r.PaymentStatus == paymentStatus);
            }
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? customerId, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Receivables.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                query = query.Where(r => r.PaymentStatus == paymentStatus);
            }
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Receivable> AddAsync(Receivable receivable, CancellationToken cancellationToken = default)
    {
        _context.Receivables.Add(receivable);
        await _context.SaveChangesAsync(cancellationToken);
        return receivable;
    }

    public async Task UpdateAsync(Receivable receivable, CancellationToken cancellationToken = default)
    {
        receivable.UpdatedAt = DateTime.UtcNow;
        _context.Receivables.Update(receivable);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetLastReceivableNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var lastReceivable = await _context.Receivables
            .Where(r => r.ReceivableNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceivableNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return lastReceivable?.ReceivableNumber ?? string.Empty;
    }

    public async Task AddPaymentAsync(ReceivablePayment payment, CancellationToken cancellationToken = default)
    {
        _context.ReceivablePayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Receivable receivable, CancellationToken cancellationToken = default)
    {
        _context.Receivables.Remove(receivable);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
