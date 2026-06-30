using FluentValidation;
using Store.Contracts.Requests.Auth;
using Store.Contracts.Requests.Users;
using Store.Contracts.Requests.Categories;
using Store.Contracts.Requests.Units;
using Store.Contracts.Requests.Products;
using Store.Contracts.Requests.Suppliers;
using Store.Contracts.Requests.Customers;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Requests.Purchases;
using Store.Contracts.Requests.Sales;
using Store.Contracts.Requests.CashSessions;
using Store.Application.Services.Store;
using Store.Domain.Enums;

namespace Store.Api.Extensions;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}

// Auth Validators
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username wajib diisi.");
    }

    public Func<object, object, Task<IEnumerable<FluentValidation.Results.ValidationFailure>>> Lambda(string property)
    {
        return (model, value) => Task.FromResult(Enumerable.Empty<FluentValidation.Results.ValidationFailure>());
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token wajib diisi.");
    }
}

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token wajib diisi.");
    }
}

// User Validators
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama wajib diisi.")
            .MaximumLength(100).WithMessage("Nama maksimal 100 karakter.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username wajib diisi.")
            .MinimumLength(3).WithMessage("Username minimal 3 karakter.")
            .MaximumLength(50).WithMessage("Username maksimal 50 karakter.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username hanya boleh berisi huruf, angka, dan underscore.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.")
            .MinimumLength(8).WithMessage("Password minimal 8 karakter.")
            .Matches("[A-Z]").WithMessage("Password harus mengandung minimal 1 huruf besar.")
            .Matches("[a-z]").WithMessage("Password harus mengandung minimal 1 huruf kecil.")
            .Matches("[0-9]").WithMessage("Password harus mengandung minimal 1 angka.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role wajib diisi.")
            .Must(role => role.ToLower() == "owner" || role.ToLower() == "admin")
            .WithMessage("Role harus 'owner' atau 'admin'.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama wajib diisi.")
            .MaximumLength(100).WithMessage("Nama maksimal 100 karakter.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role wajib diisi.")
            .Must(role => role.ToLower() == "owner" || role.ToLower() == "admin")
            .WithMessage("Role harus 'owner' atau 'admin'.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password baru wajib diisi.")
            .MinimumLength(8).WithMessage("Password minimal 8 karakter.")
            .Matches("[A-Z]").WithMessage("Password harus mengandung minimal 1 huruf besar.")
            .Matches("[a-z]").WithMessage("Password harus mengandung minimal 1 huruf kecil.")
            .Matches("[0-9]").WithMessage("Password harus mengandung minimal 1 angka.");
    }
}

// Category Validators
public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama kategori wajib diisi.")
            .MaximumLength(100).WithMessage("Nama kategori maksimal 100 karakter.");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama kategori wajib diisi.")
            .MaximumLength(100).WithMessage("Nama kategori maksimal 100 karakter.");
    }
}

// Unit Validators
public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama satuan wajib diisi.")
            .MaximumLength(50).WithMessage("Nama satuan maksimal 50 karakter.");
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama satuan wajib diisi.")
            .MaximumLength(50).WithMessage("Nama satuan maksimal 50 karakter.");
    }
}

// Product Validators
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama produk wajib diisi.")
            .MaximumLength(200).WithMessage("Nama produk maksimal 200 karakter.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU wajib diisi.")
            .MaximumLength(50).WithMessage("SKU maksimal 50 karakter.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori wajib dipilih.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Satuan wajib dipilih.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga beli tidak boleh negatif.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga jual tidak boleh negatif.")
            .GreaterThanOrEqualTo(x => x.PurchasePrice)
            .WithMessage("Harga jual harus lebih besar atau sama dengan harga beli.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok tidak boleh negatif.");

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("Barcode maksimal 50 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama produk wajib diisi.")
            .MaximumLength(200).WithMessage("Nama produk maksimal 200 karakter.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU wajib diisi.")
            .MaximumLength(50).WithMessage("SKU maksimal 50 karakter.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori wajib dipilih.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Satuan wajib dipilih.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga beli tidak boleh negatif.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga jual tidak boleh negatif.")
            .GreaterThanOrEqualTo(x => x.PurchasePrice)
            .WithMessage("Harga jual harus lebih besar atau sama dengan harga beli.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok tidak boleh negatif.");

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("Barcode maksimal 50 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}

// Supplier Validators
public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama supplier wajib diisi.")
            .MaximumLength(200).WithMessage("Nama supplier maksimal 200 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Alamat maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Catatan maksimal 1000 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama supplier wajib diisi.")
            .MaximumLength(200).WithMessage("Nama supplier maksimal 200 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Alamat maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Catatan maksimal 1000 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

// Customer Validators
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama pelanggan wajib diisi.")
            .MaximumLength(200).WithMessage("Nama pelanggan maksimal 200 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Alamat maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Catatan maksimal 1000 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama pelanggan wajib diisi.")
            .MaximumLength(200).WithMessage("Nama pelanggan maksimal 200 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Alamat maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Catatan maksimal 1000 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

// Inventory Validators
public class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.AdjustmentDate)
            .NotEmpty().WithMessage("Tanggal koreksi stok wajib diisi.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan koreksi stok wajib diisi.")
            .MaximumLength(500).WithMessage("Alasan maksimal 500 karakter.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Minimal 1 item koreksi stok.");

        RuleForEach(x => x.Items).SetValidator(new CreateStockAdjustmentItemRequestValidator());
    }
}

public class UpdateStockAdjustmentRequestValidator : AbstractValidator<UpdateStockAdjustmentRequest>
{
    public UpdateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.AdjustmentDate)
            .NotEmpty().WithMessage("Tanggal koreksi stok wajib diisi.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan koreksi stok wajib diisi.")
            .MaximumLength(500).WithMessage("Alasan maksimal 500 karakter.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Minimal 1 item koreksi stok.");

        RuleForEach(x => x.Items).SetValidator(new CreateStockAdjustmentItemRequestValidator());
    }
}

public class CreateStockAdjustmentItemRequestValidator : AbstractValidator<CreateStockAdjustmentItemRequest>
{
    public CreateStockAdjustmentItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produk wajib dipilih.");

        RuleFor(x => x.AdjustmentType)
            .NotEmpty().WithMessage("Tipe koreksi wajib diisi.")
            .Must(value => Enum.TryParse<StockAdjustmentType>(value, true, out _))
            .WithMessage("Tipe koreksi harus 'Increase' atau 'Decrease'.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity harus lebih dari 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan item maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class VoidStockAdjustmentRequestValidator : AbstractValidator<VoidStockAdjustmentRequest>
{
    public VoidStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan pembatalan wajib diisi.")
            .MaximumLength(500).WithMessage("Alasan pembatalan maksimal 500 karakter.");
    }
}

// Purchase Validators
public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Tanggal pembelian wajib diisi.");

        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier wajib dipilih.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Metode pembayaran wajib diisi.")
            .Must(value => Enum.TryParse<PaymentMethod>(value, true, out _))
            .WithMessage("Metode pembayaran tidak valid.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Diskon tidak boleh negatif.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Minimal 1 item pembelian.");

        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseItemRequestValidator());
    }
}

public class CreatePurchaseItemRequestValidator : AbstractValidator<CreatePurchaseItemRequest>
{
    public CreatePurchaseItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produk wajib dipilih.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity harus lebih dari 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Harga beli tidak boleh negatif.");
    }
}

public class VoidPurchaseRequestValidator : AbstractValidator<VoidPurchaseRequest>
{
    public VoidPurchaseRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan pembatalan wajib diisi.")
            .MaximumLength(500).WithMessage("Alasan pembatalan maksimal 500 karakter.");
    }
}

// Sale Validators
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

// Cash Session Validators
public class OpenCashSessionRequestValidator : AbstractValidator<OpenCashSessionRequest>
{
    public OpenCashSessionRequestValidator()
    {
        RuleFor(x => x.OpenedAt)
            .NotEmpty().WithMessage("Waktu buka kasir wajib diisi.");

        RuleFor(x => x.OpeningCashAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Modal kas awal tidak boleh negatif.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class CloseCashSessionRequestValidator : AbstractValidator<CloseCashSessionRequest>
{
    public CloseCashSessionRequestValidator()
    {
        RuleFor(x => x.ClosedAt)
            .NotEmpty().WithMessage("Waktu tutup kasir wajib diisi.");

        RuleFor(x => x.ActualCashAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Jumlah kas aktual tidak boleh negatif.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan maksimal 500 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

// Store Validators
public class UpdateStoreProfileRequestValidator : AbstractValidator<UpdateStoreProfileRequest>
{
    public UpdateStoreProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama toko wajib diisi.")
            .MaximumLength(100).WithMessage("Nama toko maksimal 100 karakter.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Alamat maksimal 500 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Nomor telepon maksimal 20 karakter.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Mata uang wajib diisi.")
            .MaximumLength(10).WithMessage("Mata uang maksimal 10 karakter.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Zona waktu wajib diisi.")
            .MaximumLength(50).WithMessage("Zona waktu maksimal 50 karakter.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("URL logo maksimal 500 karakter.");

        RuleFor(x => x.ReceiptFooter)
            .MaximumLength(500).WithMessage("Footer struk maksimal 500 karakter.");
    }
}

public class UpdateStoreSettingsRequestValidator : AbstractValidator<UpdateStoreSettingsRequest>
{
    public UpdateStoreSettingsRequestValidator()
    {
        RuleFor(x => x.DefaultLowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok default tidak boleh negatif.");

        RuleFor(x => x.DefaultPaymentMethod)
            .NotEmpty().WithMessage("Metode pembayaran default wajib diisi.")
            .Must(value => Enum.TryParse<PaymentMethod>(value, true, out var method)
                && (method == PaymentMethod.Cash || method == PaymentMethod.Transfer || method == PaymentMethod.QRIS))
            .WithMessage("Metode pembayaran default harus Cash, Transfer, atau QRIS.");
    }
}
