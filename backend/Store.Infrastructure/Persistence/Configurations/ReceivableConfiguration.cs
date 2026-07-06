using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Store.Domain.Entities;

namespace Store.Infrastructure.Persistence.Configurations;

public class ReceivableConfiguration : IEntityTypeConfiguration<Receivable>
{
    public void Configure(EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("receivables");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceivableNumber).IsRequired().HasMaxLength(50);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.PaidAmount).HasPrecision(18, 2);
        builder.Property(r => r.RemainingAmount).HasPrecision(18, 2);
        builder.Property(r => r.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasOne(r => r.Sale)
            .WithMany()
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReceivablePaymentConfiguration : IEntityTypeConfiguration<ReceivablePayment>
{
    public void Configure(EntityTypeBuilder<ReceivablePayment> builder)
    {
        builder.ToTable("receivable_payments");
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Amount).HasPrecision(18, 2);
        builder.Property(rp => rp.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(rp => rp.Notes).HasMaxLength(500);

        builder.HasOne(rp => rp.Receivable)
            .WithMany(r => r.Payments)
            .HasForeignKey(rp => rp.ReceivableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
