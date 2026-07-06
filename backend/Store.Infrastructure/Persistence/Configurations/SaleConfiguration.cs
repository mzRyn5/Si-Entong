using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SaleNumber).HasColumnName("sale_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleDate).HasColumnName("sale_date");
        builder.Property(x => x.CashierId).HasColumnName("cashier_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CashSessionId).HasColumnName("cash_session_id");
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
        builder.Property(x => x.AmountPaid).HasColumnName("amount_paid").HasPrecision(18, 2);
        builder.Property(x => x.ChangeAmount).HasColumnName("change_amount").HasPrecision(18, 2);
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

        builder.HasIndex(x => x.SaleNumber).IsUnique();
        builder.HasIndex(x => x.SaleDate);
        builder.HasIndex(x => x.CashierId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId);

        builder.HasOne(x => x.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(x => x.CustomerId);

        builder.HasOne(x => x.CashSession)
            .WithMany(cs => cs.Sales)
            .HasForeignKey(x => x.CashSessionId);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SaleId).HasColumnName("sale_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.ProductSnapshotName).HasColumnName("product_snapshot_name").HasMaxLength(200);
        builder.Property(x => x.ProductSnapshotSku).HasColumnName("product_snapshot_sku").HasMaxLength(50);
        builder.Property(x => x.PurchasePriceSnapshot).HasColumnName("purchase_price_snapshot").HasPrecision(18, 2);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId);
    }
}

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.CashierId).HasColumnName("cashier_id");
        builder.Property(x => x.OpenedAt).HasColumnName("opened_at");
        builder.Property(x => x.OpeningCashAmount).HasColumnName("opening_cash_amount").HasPrecision(18, 2);
        builder.Property(x => x.CashSalesAmount).HasColumnName("cash_sales_amount").HasPrecision(18, 2);
        builder.Property(x => x.CashInAmount).HasColumnName("cash_in_amount").HasPrecision(18, 2);
        builder.Property(x => x.CashOutAmount).HasColumnName("cash_out_amount").HasPrecision(18, 2);
        builder.Property(x => x.ActualCashAmount).HasColumnName("actual_cash_amount").HasPrecision(18, 2);
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at");
        builder.Property(x => x.ClosedBy).HasColumnName("closed_by");
        builder.Property(x => x.ClosingNotes).HasColumnName("closing_notes").HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.OpenedAt);
        builder.HasIndex(x => x.CashierId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId);

        builder.Ignore(x => x.ExpectedCashAmount);
        builder.Ignore(x => x.DifferenceAmount);
    }
}
