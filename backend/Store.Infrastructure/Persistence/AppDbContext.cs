using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Store.Domain.Entities;
using Store.Application.Abstractions;

namespace Store.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantProvider? _tenantProvider;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider? tenantProvider = null) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public ITenantProvider? TenantProviderInstance => _tenantProvider;

    // Auth & Users
    public DbSet<User> Users => Set<User>();
    public DbSet<StoreProfile> StoreProfiles => Set<StoreProfile>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();

    // Master Data
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();

    // Inventory
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();
    public DbSet<StockOpname> StockOpnames => Set<StockOpname>();
    public DbSet<StockOpnameItem> StockOpnameItems => Set<StockOpnameItem>();

    // Transactions
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    // Expenses
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // Payables
    public DbSet<Payable> Payables => Set<Payable>();
    public DbSet<PayablePayment> PayablePayments => Set<PayablePayment>();

    // Receivables
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<ReceivablePayment> ReceivablePayments => Set<ReceivablePayment>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // AI Chat
    public DbSet<AiChatSession> AiChatSessions => Set<AiChatSession>();
    public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();
    public DbSet<AiActionDraft> AiActionDrafts => Set<AiActionDraft>();
    public DbSet<AiActionLog> AiActionLogs => Set<AiActionLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicitly map AuditLog to User relationship
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);
        });

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global tenant config and column mapping
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Domain.Common.ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<Guid>("StoreId")
                    .HasColumnName("store_id")
                    .IsRequired();
                
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("StoreId");
            }
            else if (typeof(Domain.Common.IOptionalTenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<Guid?>("StoreId")
                    .HasColumnName("store_id")
                    .IsRequired(false);
                
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("StoreId");
            }
        }

        // Global query filters for soft delete and tenant isolation
        var applyFiltersMethod = typeof(AppDbContext).GetMethod(nameof(ApplyGlobalFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType == null) // only apply to root entity types
            {
                var genericMethod = applyFiltersMethod?.MakeGenericMethod(entityType.ClrType);
                genericMethod?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ApplyGlobalFilters<TEntity>(ModelBuilder modelBuilder) where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? expression = null;

        // Soft delete check
        if (typeof(Domain.Common.SoftDeletableEntity).IsAssignableFrom(typeof(TEntity)))
        {
            var isDeletedProp = Expression.Property(parameter, nameof(Domain.Common.SoftDeletableEntity.IsDeleted));
            var comparison = Expression.Equal(isDeletedProp, Expression.Constant(false));
            expression = comparison;
        }

        // Tenant check
        if (typeof(Domain.Common.ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            var tenantIdProp = Expression.Property(parameter, "StoreId");
            var providerCheck = Expression.Property(Expression.Constant(this), nameof(TenantProviderInstance));
            var isSysAdminProp = Expression.Property(providerCheck, nameof(ITenantProvider.IsSysAdmin));
            var tenantIdValue = Expression.Property(providerCheck, nameof(ITenantProvider.TenantId));
            
            var hasValueProp = Expression.Property(tenantIdValue, "HasValue");
            var valueProp = Expression.Property(tenantIdValue, "Value");
            
            var conditionValue = Expression.Condition(
                hasValueProp,
                valueProp,
                Expression.Constant(Guid.Empty)
            );

            var tenantComparison = Expression.Equal(tenantIdProp, conditionValue);
            var bypassExpression = Expression.OrElse(
                Expression.Equal(providerCheck, Expression.Constant(null)),
                isSysAdminProp
            );
            
            var tenantFilter = Expression.OrElse(bypassExpression, tenantComparison);
            expression = expression != null ? Expression.AndAlso(expression, tenantFilter) : tenantFilter;
        }
        else if (typeof(Domain.Common.IOptionalTenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            var tenantIdProp = Expression.Property(parameter, "StoreId");
            var providerCheck = Expression.Property(Expression.Constant(this), nameof(TenantProviderInstance));
            var isSysAdminProp = Expression.Property(providerCheck, nameof(ITenantProvider.IsSysAdmin));
            var tenantIdValue = Expression.Property(providerCheck, nameof(ITenantProvider.TenantId));

            var tenantComparison = Expression.Equal(tenantIdProp, tenantIdValue);
            var bypassExpression = Expression.OrElse(
                Expression.Equal(providerCheck, Expression.Constant(null)),
                isSysAdminProp
            );

            var tenantFilter = Expression.OrElse(bypassExpression, tenantComparison);
            expression = expression != null ? Expression.AndAlso(expression, tenantFilter) : tenantFilter;
        }

        if (expression != null)
        {
            var lambda = Expression.Lambda(expression, parameter);
            modelBuilder.Entity<TEntity>().HasQueryFilter(lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        SetTenantId();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTenantId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void SetTenantId()
    {
        if (_tenantProvider == null) return;

        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue || tenantId == Guid.Empty) return;

        var entries = ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is Domain.Common.ITenantEntity tenantEntity)
                {
                    if (tenantEntity.StoreId == Guid.Empty)
                    {
                        tenantEntity.StoreId = tenantId.Value;
                    }
                }
                else if (entry.Entity is Domain.Common.IOptionalTenantEntity optionalTenantEntity)
                {
                    if (!optionalTenantEntity.StoreId.HasValue || optionalTenantEntity.StoreId == Guid.Empty)
                    {
                        optionalTenantEntity.StoreId = tenantId.Value;
                    }
                }
            }
        }
    }
}
