using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Categories;
using Store.Contracts.Requests.Units;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Categories;
using Store.Contracts.Responses.Units;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.MasterData;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryResponse>> GetAllForDropdownAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
}

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CategoryService(ICategoryRepository categoryRepository, IAuditLogRepository auditLogRepository)
    {
        _categoryRepository = categoryRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<CategoryResponse>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        var totalCount = await _categoryRepository.GetTotalCountAsync(search, cancellationToken);

        var responses = categories.Select(MapToResponse).ToList();

        return new PagedResponse<CategoryResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllForDropdownAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllForDropdownAsync(cancellationToken);
        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return category == null ? null : MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (await _categoryRepository.NameExistsAsync(request.Name, null, cancellationToken))
        {
            throw new BusinessRuleException("Nama kategori sudah digunakan.", "DUPLICATE_DATA");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.AddAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Category",
            EntityId = created.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<CategoryResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null) return null;

        if (await _categoryRepository.NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new BusinessRuleException("Nama kategori sudah digunakan.", "DUPLICATE_DATA");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.UpdatedBy = updatedBy;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "Category",
            EntityId = category.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(category);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null) return false;

        if (await _categoryRepository.HasActiveProductsAsync(id, cancellationToken))
        {
            throw new BusinessRuleException("Kategori masih digunakan oleh produk aktif.", "HAS_ACTIVE_PRODUCTS");
        }

        category.IsActive = false;
        category.UpdatedBy = deletedBy;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Deactivate",
            EntityName = "Category",
            EntityId = category.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static CategoryResponse MapToResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt
    };
}

public interface IUnitService
{
    Task<PagedResponse<UnitResponse>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<UnitResponse>> GetAllForDropdownAsync(CancellationToken cancellationToken = default);
    Task<UnitResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitResponse> CreateAsync(CreateUnitRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<UnitResponse?> UpdateAsync(Guid id, UpdateUnitRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
}

public class UnitService : IUnitService
{
    private readonly IUnitRepository _unitRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UnitService(IUnitRepository unitRepository, IAuditLogRepository auditLogRepository)
    {
        _unitRepository = unitRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<UnitResponse>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        var totalCount = await _unitRepository.GetTotalCountAsync(search, cancellationToken);

        var responses = units.Select(MapToResponse).ToList();

        return new PagedResponse<UnitResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<IEnumerable<UnitResponse>> GetAllForDropdownAsync(CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllForDropdownAsync(cancellationToken);
        return units.Select(MapToResponse);
    }

    public async Task<UnitResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
        return unit == null ? null : MapToResponse(unit);
    }

    public async Task<UnitResponse> CreateAsync(CreateUnitRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (await _unitRepository.NameExistsAsync(request.Name, null, cancellationToken))
        {
            throw new BusinessRuleException("Nama satuan sudah digunakan.", "DUPLICATE_DATA");
        }

        var unit = new Unit
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _unitRepository.AddAsync(unit, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Unit",
            EntityId = created.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<UnitResponse?> UpdateAsync(Guid id, UpdateUnitRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
        if (unit == null) return null;

        if (await _unitRepository.NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new BusinessRuleException("Nama satuan sudah digunakan.", "DUPLICATE_DATA");
        }

        unit.Name = request.Name;
        unit.Description = request.Description;
        unit.IsActive = request.IsActive;
        unit.UpdatedBy = updatedBy;
        unit.UpdatedAt = DateTime.UtcNow;

        await _unitRepository.UpdateAsync(unit, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "Unit",
            EntityId = unit.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(unit);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
        if (unit == null) return false;

        if (await _unitRepository.HasActiveProductsAsync(id, cancellationToken))
        {
            throw new BusinessRuleException("Satuan masih digunakan oleh produk aktif.", "HAS_ACTIVE_PRODUCTS");
        }

        unit.IsActive = false;
        unit.UpdatedBy = deletedBy;
        unit.UpdatedAt = DateTime.UtcNow;

        await _unitRepository.UpdateAsync(unit, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Deactivate",
            EntityName = "Unit",
            EntityId = unit.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static UnitResponse MapToResponse(Unit unit) => new()
    {
        Id = unit.Id,
        Name = unit.Name,
        Description = unit.Description,
        IsActive = unit.IsActive,
        CreatedAt = unit.CreatedAt
    };
}
