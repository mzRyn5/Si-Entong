using Store.Domain.Entities;
using Store.Contracts.Responses.Reports;

namespace Store.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllForDropdownAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public interface IUnitRepository
{
    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Unit>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<IEnumerable<Unit>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Unit>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Unit>> GetAllForDropdownAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<Unit> AddAsync(Unit unit, CancellationToken cancellationToken = default);
    Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveProductsAsync(Guid unitId, CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(string? search, Guid? categoryId, bool? isActive, bool? isLowStock, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllForPosAsync(string? search, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default);
    Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> BarcodeExistsAsync(string? barcode, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActivePurchasesAsync(Guid supplierId, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface IStockMovementRepository
{
    Task<IEnumerable<StockMovement>> GetAllAsync(Guid? productId, DateTimeOffset? fromDate, DateTimeOffset? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? productId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken cancellationToken = default);
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);
}

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Purchase?> GetByNumberAsync(string purchaseNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Purchase>> GetAllAsync(Guid? supplierId, DateTimeOffset? fromDate, DateTimeOffset? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? supplierId, DateTimeOffset? fromDate, DateTimeOffset? toDate, string? status, CancellationToken cancellationToken = default);
    Task<Purchase> AddAsync(Purchase purchase, CancellationToken cancellationToken = default);
    Task UpdateAsync(Purchase purchase, CancellationToken cancellationToken = default);
    Task DeleteAsync(Purchase purchase, CancellationToken cancellationToken = default);
    Task AddItemAsync(PurchaseItem item, CancellationToken cancellationToken = default);
    Task<string> GeneratePurchaseNumberAsync(CancellationToken cancellationToken = default);
}

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Sale>> GetAllAsync(Guid? cashierId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? cashierId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default);
    Task<Sale> AddAsync(Sale sale, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task DeleteAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<string> GenerateSaleNumberAsync(CancellationToken cancellationToken = default);
}

public interface ICashSessionRepository
{
    Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashSession?> GetActiveByCashierIdAsync(Guid cashierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashSession>> GetAllAsync(Guid? cashierId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? cashierId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default);
    Task<CashSession> AddAsync(CashSession cashSession, CancellationToken cancellationToken = default);
    Task UpdateAsync(CashSession cashSession, CancellationToken cancellationToken = default);
}

public interface IStoreProfileRepository
{
    Task<StoreProfile?> GetAsync(CancellationToken cancellationToken = default);
    Task<StoreProfile> AddAsync(StoreProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoreProfile profile, CancellationToken cancellationToken = default);
}

public interface IStoreSettingsRepository
{
    Task<StoreSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task<StoreSettings> AddAsync(StoreSettings settings, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoreSettings settings, CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAllAsync(Guid? userId, string? action, string? module, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? userId, string? action, string? module, CancellationToken cancellationToken = default);
}

public interface IReportRepository
{
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<DailySalesReportResponse>> GetDailySalesAsync(DateTime fromDate, DateTime toDate, string? paymentMethod = null, Guid? productId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductSalesReportResponse>> GetProductSalesAsync(DateTime fromDate, DateTime toDate, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockValuationReportResponse>> GetStockValuationAsync(Guid? categoryId, CancellationToken cancellationToken = default);
    Task<BasicProfitReportResponse> GetBasicProfitAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReportResponse>> GetPurchasesAsync(DateTime fromDate, DateTime toDate, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseReportResponse>> GetExpensesAsync(DateTime fromDate, DateTime toDate, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovementReportResponse>> GetInventoryMovementsAsync(DateTime fromDate, DateTime toDate, Guid? productId, string? movementType, CancellationToken cancellationToken = default);
}

public interface IStockAdjustmentRepository
{
    Task<StockAdjustment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockAdjustment?> GetByNumberAsync(string adjustmentNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockAdjustment>> GetAllAsync(DateTimeOffset? fromDate, DateTimeOffset? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(DateTimeOffset? fromDate, DateTimeOffset? toDate, string? status, CancellationToken cancellationToken = default);
    Task<StockAdjustment> AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default);
    Task<string> GenerateAdjustmentNumberAsync(CancellationToken cancellationToken = default);
}

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseCategory>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseCategory>> GetAllForDropdownAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search, CancellationToken cancellationToken = default);
    Task<ExpenseCategory> AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExpenseCategory category, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Expense>> GetAllAsync(Guid? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? categoryId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<Expense> AddAsync(Expense expense, CancellationToken cancellationToken = default);
    Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default);
    Task<string> GenerateExpenseNumberAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default);
}

public interface ICashMovementRepository
{
    Task<CashMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashMovement>> GetAllAsync(Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<CashMovement> AddAsync(CashMovement movement, CancellationToken cancellationToken = default);
}

public interface ISalesReturnRepository
{
    Task<SalesReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesReturn>> GetAllAsync(Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? saleId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default);
    Task<SalesReturn> AddAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default);
    Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken = default);
}

public interface IPurchaseReturnRepository
{
    Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReturn>> GetAllAsync(Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? purchaseId, DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default);
    Task<PurchaseReturn> AddAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default);
    Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken = default);
}

public interface IStockOpnameRepository
{
    Task<StockOpname?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockOpname>> GetAllAsync(DateTime? fromDate, DateTime? toDate, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(DateTime? fromDate, DateTime? toDate, string? status, CancellationToken cancellationToken = default);
    Task<StockOpname> AddAsync(StockOpname opname, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockOpname opname, CancellationToken cancellationToken = default);
    Task<string> GenerateOpnameNumberAsync(CancellationToken cancellationToken = default);
}

