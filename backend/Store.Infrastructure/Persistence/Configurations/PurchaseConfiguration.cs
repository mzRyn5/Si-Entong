using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PurchaseNumber).HasColumnName("purchase_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PurchaseDate).HasColumnName("purchase_date");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasConversion<string>();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.VoidedAt).HasColumnName("voided_at");
        builder.Property(x => x.VoidedBy).HasColumnName("voided_by");
        builder.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.PurchaseNumber).IsUnique();
        builder.HasIndex(x => x.PurchaseDate);
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(x => x.SupplierId);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.Purchase)
            .HasForeignKey(i => i.PurchaseId);
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PurchaseId).HasColumnName("purchase_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.ProductSnapshotName).HasColumnName("product_snapshot_name").HasMaxLength(200);
        builder.Property(x => x.ProductSnapshotSku).HasColumnName("product_snapshot_sku").HasMaxLength(50);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId);
    }
}
