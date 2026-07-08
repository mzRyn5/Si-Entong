using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(x => new { x.StoreId, x.Name }).IsUnique();
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ExpenseNumber).HasColumnName("expense_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExpenseDate).HasColumnName("expense_date");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.VoidedAt).HasColumnName("voided_at");
        builder.Property(x => x.VoidedBy).HasColumnName("voided_by");
        builder.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.ExpenseNumber).IsUnique();
        builder.HasIndex(x => x.ExpenseDate);
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(x => x.CategoryId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(100);
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.OldValues).HasColumnName("old_values");
        builder.Property(x => x.NewValues).HasColumnName("new_values");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.Module);
    }
}
