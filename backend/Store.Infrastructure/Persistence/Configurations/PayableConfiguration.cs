using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Configurations;

public class PayableConfiguration : IEntityTypeConfiguration<Payable>
{
    public void Configure(EntityTypeBuilder<Payable> builder)
    {
        builder.ToTable("payables");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PayableNumber).IsRequired().HasMaxLength(50);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 2);
        builder.Property(p => p.PaidAmount).HasPrecision(18, 2);
        builder.Property(p => p.RemainingAmount).HasPrecision(18, 2);
        builder.Property(p => p.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Purchase)
            .WithMany()
            .HasForeignKey(p => p.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayablePaymentConfiguration : IEntityTypeConfiguration<PayablePayment>
{
    public void Configure(EntityTypeBuilder<PayablePayment> builder)
    {
        builder.ToTable("payable_payments");
        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Amount).HasPrecision(18, 2);
        builder.Property(pp => pp.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(pp => pp.Notes).HasMaxLength(500);

        builder.HasOne(pp => pp.Payable)
            .WithMany(p => p.Payments)
            .HasForeignKey(pp => pp.PayableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
