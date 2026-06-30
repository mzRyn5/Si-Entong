using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Expenses;
using Store.Contracts.Requests.Expenses;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Expenses;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExpensesController : BaseApiController
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get paged expense categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(PagedResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetAllCategoriesAsync(search, isActive, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get expense categories for dropdown
    /// </summary>
    [HttpGet("categories/dropdown")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesDropdown(CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetCategoriesForDropdownAsync(cancellationToken);
        return SuccessResponse(result, "Kategori pengeluaran berhasil diambil.");
    }

    /// <summary>
    /// Get expense category by ID
    /// </summary>
    [HttpGet("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetCategoryByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Kategori pengeluaran", id);
        }
        return SuccessResponse(result, "Kategori pengeluaran berhasil diambil.");
    }

    /// <summary>
    /// Create new expense category
    /// </summary>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.CreateCategoryAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Kategori pengeluaran berhasil dibuat.");
    }

    /// <summary>
    /// Update expense category
    /// </summary>
    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.UpdateCategoryAsync(id, request, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Kategori pengeluaran", id);
        }
        return SuccessResponse(result, "Kategori pengeluaran berhasil diperbarui.");
    }

    /// <summary>
    /// Delete expense category
    /// </summary>
    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _expenseService.DeleteCategoryAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Kategori pengeluaran", id);
        }
        return SuccessResponse<object>(null!, "Kategori pengeluaran berhasil dihapus.");
    }

    /// <summary>
    /// Get paged expenses
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetAllExpensesAsync(categoryId, fromDate, toDate, page, pageSize, cancellationToken);
        return PagedResponse(result);
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.GetExpenseByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pengeluaran", id);
        }
        return SuccessResponse(result, "Pengeluaran berhasil diambil.");
    }

    /// <summary>
    /// Create new expense record
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.CreateExpenseAsync(request, CurrentUserId, cancellationToken);
        return CreatedResponse(result, "Pengeluaran berhasil dicatat.");
    }

    /// <summary>
    /// Void an expense record
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoidExpense(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _expenseService.VoidExpenseAsync(id, CurrentUserId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Pengeluaran", id);
        }
        return SuccessResponse(result, "Pengeluaran berhasil dibatalkan (void).");
    }

    /// <summary>
    /// Delete an expense record
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _expenseService.DeleteExpenseAsync(id, CurrentUserId, cancellationToken);
        if (!success)
        {
            return NotFoundResponse("Pengeluaran", id);
        }
        return SuccessResponse<object>(null!, "Pengeluaran berhasil dihapus.");
    }
}
