using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Expense>> GetAllAsync(
        Guid? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(Guid? categoryId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses.AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= toDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Expense> AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public async Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        expense.UpdatedAt = DateTime.UtcNow;
        _context.Expenses.Update(expense);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateExpenseNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"EXP-{today:yyyyMMdd}-";

        var lastNumber = await _context.Expenses
            .IgnoreQueryFilters()
            .Where(e => e.ExpenseNumber.StartsWith(prefix))
            .OrderByDescending(e => e.ExpenseNumber)
            .Select(e => e.ExpenseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
        {
            return $"{prefix}0001";
        }

        var lastSequence = int.Parse(lastNumber.Split('-').Last());
        return $"{prefix}{(lastSequence + 1):D4}";
    }
}
