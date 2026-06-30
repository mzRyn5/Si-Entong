using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Expenses;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Expenses;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.Expenses;

public interface IExpenseService
{
    Task<PagedResponse<ExpenseCategoryResponse>> GetAllCategoriesAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseCategoryResponse>> GetCategoriesForDropdownAsync(CancellationToken cancellationToken = default);
    Task<ExpenseCategoryResponse?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryResponse> CreateCategoryAsync(CreateExpenseCategoryRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryResponse?> UpdateCategoryAsync(Guid id, UpdateExpenseCategoryRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);

    Task<PagedResponse<ExpenseResponse>> GetAllExpensesAsync(Guid? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ExpenseResponse?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ExpenseResponse?> VoidExpenseAsync(Guid id, Guid voidedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteExpenseAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

public class ExpenseService : IExpenseService
{
    private readonly IExpenseCategoryRepository _categoryRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ExpenseService(
        IExpenseCategoryRepository categoryRepository,
        IExpenseRepository expenseRepository,
        IAuditLogRepository auditLogRepository)
    {
        _categoryRepository = categoryRepository;
        _expenseRepository = expenseRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<ExpenseCategoryResponse>> GetAllCategoriesAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        var totalCount = await _categoryRepository.GetTotalCountAsync(search, cancellationToken);

        var responses = categories.Select(MapToCategoryResponse).ToList();

        return new PagedResponse<ExpenseCategoryResponse>
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

    public async Task<IEnumerable<ExpenseCategoryResponse>> GetCategoriesForDropdownAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllForDropdownAsync(cancellationToken);
        return categories.Select(MapToCategoryResponse);
    }

    public async Task<ExpenseCategoryResponse?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return category == null ? null : MapToCategoryResponse(category);
    }

    public async Task<ExpenseCategoryResponse> CreateCategoryAsync(
        CreateExpenseCategoryRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (await _categoryRepository.NameExistsAsync(request.Name, null, cancellationToken))
        {
            throw new BusinessRuleException("Nama kategori pengeluaran sudah digunakan.", "DUPLICATE_DATA");
        }

        var category = new ExpenseCategory
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.AddAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "ExpenseCategory",
            EntityId = created.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToCategoryResponse(created);
    }

    public async Task<ExpenseCategoryResponse?> UpdateCategoryAsync(
        Guid id, UpdateExpenseCategoryRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null) return null;

        if (await _categoryRepository.NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new BusinessRuleException("Nama kategori pengeluaran sudah digunakan.", "DUPLICATE_DATA");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.UpdatedBy = updatedBy;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "ExpenseCategory",
            EntityId = category.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToCategoryResponse(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null) return false;

        category.IsDeleted = true;
        category.DeletedBy = deletedBy;
        category.DeletedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Delete",
            EntityName = "ExpenseCategory",
            EntityId = category.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    public async Task<PagedResponse<ExpenseResponse>> GetAllExpensesAsync(
        Guid? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenseRepository.GetAllAsync(categoryId, fromDate, toDate, page, pageSize, cancellationToken);
        var totalCount = await _expenseRepository.GetTotalCountAsync(categoryId, fromDate, toDate, cancellationToken);

        var responses = expenses.Select(MapToExpenseResponse).ToList();

        return new PagedResponse<ExpenseResponse>
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

    public async Task<ExpenseResponse?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        return expense == null ? null : MapToExpenseResponse(expense);
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(
        CreateExpenseRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.ExpenseCategoryId, cancellationToken);
        if (category == null)
        {
            throw new BusinessRuleException("Kategori pengeluaran tidak ditemukan.", "NOT_FOUND");
        }

        var expenseNumber = await _expenseRepository.GenerateExpenseNumberAsync(cancellationToken);

        var expense = new Expense
        {
            ExpenseNumber = expenseNumber,
            CategoryId = request.ExpenseCategoryId,
            Amount = request.Amount,
            Notes = request.Description,
            ExpenseDate = request.ExpenseDate,
            Status = Domain.Enums.TransactionStatus.Posted,
            PaymentMethod = Enum.TryParse<Domain.Enums.PaymentMethod>(request.PaymentMethod, true, out var pm) ? pm : Domain.Enums.PaymentMethod.Cash,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _expenseRepository.AddAsync(expense, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Expense",
            EntityId = created.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToExpenseResponse(created);
    }

    public async Task<ExpenseResponse?> VoidExpenseAsync(
        Guid id, Guid voidedBy, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        if (expense == null) return null;

        if (expense.Status == Domain.Enums.TransactionStatus.Voided)
        {
            throw new BusinessRuleException("Pengeluaran sudah divoid.", "ALREADY_VOIDED");
        }

        expense.Status = Domain.Enums.TransactionStatus.Voided;
        expense.VoidedAt = DateTime.UtcNow;
        expense.VoidedBy = voidedBy;
        expense.VoidReason = "Voided by user";
        expense.UpdatedBy = voidedBy;
        expense.UpdatedAt = DateTime.UtcNow;

        await _expenseRepository.UpdateAsync(expense, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = voidedBy,
            Action = "Void",
            EntityName = "Expense",
            EntityId = expense.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToExpenseResponse(expense);
    }

    public async Task<bool> DeleteExpenseAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        if (expense == null) return false;

        await _expenseRepository.DeleteAsync(expense, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Delete",
            EntityName = "Expense",
            EntityId = expense.Id,
            Module = "Expenses",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private ExpenseCategoryResponse MapToCategoryResponse(ExpenseCategory category)
    {
        return new ExpenseCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }

    private ExpenseResponse MapToExpenseResponse(Expense expense)
    {
        return new ExpenseResponse
        {
            Id = expense.Id,
            ExpenseCategoryId = expense.CategoryId,
            CategoryName = expense.Category?.Name ?? string.Empty,
            ExpenseNumber = expense.ExpenseNumber,
            Amount = expense.Amount,
            PaymentMethod = expense.PaymentMethod.ToString(),
            Status = expense.Status == Domain.Enums.TransactionStatus.Voided ? "Voided" : "Active",
            Description = expense.Notes ?? string.Empty,
            ExpenseDate = expense.ExpenseDate,
            VoidedAt = expense.VoidedAt,
            VoidReason = expense.VoidReason
        };
    }
}
