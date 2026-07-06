using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Repositories;

public class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly AppDbContext _context;

    public ExpenseCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseCategories
            .FirstOrDefaultAsync(ec => ec.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ExpenseCategory>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ExpenseCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ec => ec.Name.Contains(search) || (ec.Description != null && ec.Description.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(ec => ec.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(ec => ec.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExpenseCategory>> GetAllForDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseCategories
            .Where(ec => ec.IsActive)
            .OrderBy(ec => ec.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.ExpenseCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ec => ec.Name.Contains(search) || (ec.Description != null && ec.Description.Contains(search)));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<ExpenseCategory> AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.ExpenseCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ExpenseCategories.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(ec => ec.Id != excludeId.Value);
        }

        return await query.AnyAsync(ec => ec.Name.ToLower() == name.ToLower(), cancellationToken);
    }
}
