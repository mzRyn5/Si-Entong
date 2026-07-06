using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Repositories;

public class PayableRepository : IPayableRepository
{
    private readonly AppDbContext _context;

    public PayableRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payables
            .Include(p => p.Supplier)
            .Include(p => p.Purchase)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Payable>> GetAllAsync(Guid? supplierId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Payables.Include(p => p.Supplier).Include(p => p.Purchase).AsNoTracking().AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? supplierId, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Payables.AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Payable> AddAsync(Payable payable, CancellationToken cancellationToken = default)
    {
        _context.Payables.Add(payable);
        await _context.SaveChangesAsync(cancellationToken);
        return payable;
    }

    public async Task UpdateAsync(Payable payable, CancellationToken cancellationToken = default)
    {
        payable.UpdatedAt = DateTime.UtcNow;
        _context.Payables.Update(payable);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetLastPayableNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var lastPayable = await _context.Payables
            .Where(p => p.PayableNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PayableNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return lastPayable?.PayableNumber ?? string.Empty;
    }

    public async Task AddPaymentAsync(PayablePayment payment, CancellationToken cancellationToken = default)
    {
        _context.PayablePayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePaymentAsync(PayablePayment payment, CancellationToken cancellationToken = default)
    {
        _context.PayablePayments.Remove(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Payable payable, CancellationToken cancellationToken = default)
    {
        _context.Payables.Remove(payable);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
