using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Store.Application.Abstractions;
using Store.Application.Abstractions.Repositories;
using Store.Application.Abstractions.Services;
using Store.Application.Services.AiChat;
using Store.Application.Services.AiChat.Tools;
using Store.Application.Services.Auth;
using Store.Application.Services.Inventory;
using Store.Application.Services.MasterData;
using Store.Application.Services.Purchases;
using Store.Application.Services.Reports;
using Store.Application.Services.Sales;
using Store.Application.Services.Store;
using Store.Application.Services.Users;
using Store.Application.Services.Expenses;
using Store.Application.Services.CashMovements;
using Store.Application.Services.AuditLogs;
using Store.Application.Services.Returns;
using Store.Application.Services.Payables;
using Store.Application.Services.Receivables;
using Store.Infrastructure.Authentication.Abstractions;
using Store.Infrastructure.Authentication;
using Store.Infrastructure.Persistence.Repositories;
using Store.Infrastructure.Services.Gemini;

namespace Store.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register JWT service
        services.AddScoped<Authentication.Abstractions.IJwtService, JwtService>();
        services.AddScoped<Store.Application.Abstractions.IJwtService>(
            provider => provider.GetRequiredService<Authentication.Abstractions.IJwtService>());

        // Register HttpContextAccessor and TenantProvider
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, Store.Infrastructure.Services.TenantProvider>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<Store.Application.Abstractions.Repositories.ICategoryRepository, CategoryRepository>();
        services.AddScoped<Store.Application.Abstractions.Repositories.IUnitRepository, UnitRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ICashSessionRepository, CashSessionRepository>();
        services.AddScoped<IStoreProfileRepository, StoreProfileRepository>();
        services.AddScoped<IStoreSettingsRepository, StoreSettingsRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<ISalesReturnRepository, SalesReturnRepository>();
        services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
        services.AddScoped<IStockOpnameRepository, StockOpnameRepository>();
        services.AddScoped<IPayableRepository, PayableRepository>();
        services.AddScoped<IReceivableRepository, ReceivableRepository>();
        services.AddScoped<IAiChatRepository, AiChatRepository>();

        // Register services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ICashSessionService, CashSessionService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICashMovementService, CashMovementService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IStockOpnameService, StockOpnameService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IPayableService, PayableService>();
        services.AddScoped<IReceivableService, ReceivableService>();

        // Register HTTP Client and Gemini Client
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IGeminiClient, Store.Infrastructure.Services.Gemini.GeminiClientService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        });

        // Register AI Services
        services.AddScoped<AiInputGuard>();
        services.AddScoped<AiAgentLoop>();
        services.AddScoped<AiResponseComposer>();
        
        // Register Tool Handlers
        services.AddScoped<IAiToolHandler, ProductToolHandler>();
        services.AddScoped<IAiToolHandler, SalesPurchaseDraftToolHandler>();
        services.AddScoped<IAiToolHandler, PartnerDebtToolHandler>();
        services.AddScoped<IAiToolHandler, InventoryMasterToolHandler>();
        services.AddScoped<IAiToolHandler, ReportToolHandler>();
        services.AddScoped<IAiToolHandler, UiToolHandler>();
        services.AddScoped<AiToolRegistry>();

        services.AddScoped<IAiToolExecutor, AiToolExecutor>();
        services.AddScoped<IAiDraftActionService, AiDraftActionService>();
        services.AddScoped<IAiChatService, AiChatService>();

        // Register Background Service for AI Chat Cleanup
        services.AddHostedService<Store.Infrastructure.Services.Background.AiChatCleanupBackgroundService>();

        return services;
    }
}
