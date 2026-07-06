using FluentValidation;
using Store.Contracts.Requests.Sales;
using Store.Domain.Enums;

namespace Store.Api.Validators.Sales;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.SaleDate)
            .NotEmpty().WithMessage("Tanggal penjualan wajib diisi.");

        RuleFor(x => x.PaymentMethod)
            .Must(value => value == PaymentMethod.Cash || value == PaymentMethod.Transfer || value == PaymentMethod.QRIS)
            .WithMessage("Metode pembayaran harus Cash, Transfer, atau QRIS.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Diskon tidak boleh negatif.");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Pajak tidak boleh negatif.");

        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("Uang diterima tidak boleh negatif.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Minimal 1 item penjualan.");

        RuleForEach(x => x.Items).SetValidator(new CreateSaleItemRequestValidator());
    }
}

public class CreateSaleItemRequestValidator : AbstractValidator<CreateSaleItemRequest>
{
    public CreateSaleItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produk wajib dipilih.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity harus lebih dari 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga jual tidak boleh negatif.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Diskon item tidak boleh negatif.");
    }
}

public class VoidSaleRequestValidator : AbstractValidator<VoidSaleRequest>
{
    public VoidSaleRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan pembatalan wajib diisi.")
            .MaximumLength(500).WithMessage("Alasan pembatalan maksimal 500 karakter.");
    }
}
