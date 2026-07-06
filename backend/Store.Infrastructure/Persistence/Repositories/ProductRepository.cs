using Microsoft.EntityFrameworkCore;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Infrastructure.Persistence;

namespace Store.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(
        string? search, Guid? categoryId, bool? isActive, bool? isLowStock,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search) || (p.Barcode != null && p.Barcode.Contains(search)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (isLowStock == true)
        {
            query = query.Where(p => p.IsActive && p.CurrentStock > 0 && p.CurrentStock <= p.LowStockThreshold);
        }

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllForPosAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.IsActive && !p.IsDeleted && p.CurrentStock > 0)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search) || (p.Barcode != null && p.Barcode.Contains(search)));
        }

        return await query.OrderBy(p => p.Name).Take(50).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.IsActive && !p.IsDeleted && p.CurrentStock > 0 && p.CurrentStock <= p.LowStockThreshold)
            .AsNoTracking()
            .OrderBy(p => p.CurrentStock)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? search, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .CountAsync(p => p.IsActive && !p.IsDeleted && p.CurrentStock > 0 && p.CurrentStock <= p.LowStockThreshold, cancellationToken);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => p.Sku == sku);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> BarcodeExistsAsync(string? barcode, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;

        var query = _context.Products.Where(p => p.Barcode == barcode);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }
}
