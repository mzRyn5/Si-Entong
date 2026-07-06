using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Infrastructure.Persistence;

namespace Store.Infrastructure.Persistence.Repositories;

public class CategoryRepository : Store.Application.Abstractions.Repositories.ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        return await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllForDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.Where(c => c.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.CategoryId == categoryId && p.IsActive && !p.IsDeleted, cancellationToken);
    }
}

public class UnitRepository : Store.Application.Abstractions.Repositories.IUnitRepository
{
    private readonly AppDbContext _context;

    public UnitRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query.OrderBy(u => u.Name).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(search));
        }

        return await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetAllForDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Units
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(search));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Unit> AddAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        _context.Units.Add(unit);
        await _context.SaveChangesAsync(cancellationToken);
        return unit;
    }

    public async Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        unit.UpdatedAt = DateTime.UtcNow;
        _context.Units.Update(unit);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.Where(u => u.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveProductsAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.UnitId == unitId && p.IsActive && !p.IsDeleted, cancellationToken);
    }
}
