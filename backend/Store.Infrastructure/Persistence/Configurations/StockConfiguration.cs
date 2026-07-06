using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.MovementDate).HasColumnName("movement_date");
        builder.Property(x => x.MovementType).HasColumnName("movement_type").HasConversion<string>();
        builder.Property(x => x.QuantityBefore).HasColumnName("quantity_before");
        builder.Property(x => x.QuantityChange).HasColumnName("quantity_change");
        builder.Property(x => x.QuantityAfter).HasColumnName("quantity_after");
        builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id");
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.MovementDate);
        builder.HasIndex(x => x.MovementType);

        builder.HasOne(x => x.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(x => x.ProductId);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("stock_adjustments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AdjustmentNumber).HasColumnName("adjustment_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.AdjustmentDate).HasColumnName("adjustment_date");
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.VoidedAt).HasColumnName("voided_at");
        builder.Property(x => x.VoidedBy).HasColumnName("voided_by");
        builder.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.AdjustmentNumber).IsUnique();
        builder.HasIndex(x => x.AdjustmentDate);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.StockAdjustment)
            .HasForeignKey(i => i.StockAdjustmentId);
    }
}

public class StockAdjustmentItemConfiguration : IEntityTypeConfiguration<StockAdjustmentItem>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentItem> builder)
    {
        builder.ToTable("stock_adjustment_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.StockAdjustmentId).HasColumnName("stock_adjustment_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.AdjustmentType).HasColumnName("adjustment_type").HasConversion<string>();
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId);
    }
}
