using Microsoft.EntityFrameworkCore;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        var sysadminExists = await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == "sysadmin");
        if (sysadminExists)
        {
            return; // Database has been seeded
        }

        // Clean up old data to avoid duplicate key issues in multi-store seed
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE users, store_profiles, store_settings, products, categories, " +
                "units, suppliers, customers, stock_movements, purchases, purchase_items, " +
                "sales, sale_items, cash_sessions, cash_movements, sales_returns, " +
                "sales_return_items, purchase_returns, expense_categories, expenses, " +
                "payables, payable_payments, receivables CASCADE;");
        }
        catch
        {
            // Ignore if some tables do not exist yet
        }

        var storeAId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var storeBId = Guid.Parse("b0000000-0000-0000-0000-000000000002");

        // Seed Sys Admin (No Store)
        var sysadmin = new User
        {
            Id = Guid.Parse("d0000000-0000-0000-0000-000000000000"),
            Name = "System Administrator",
            Username = "sysadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SysAdmin123!"),
            Role = UserRole.SysAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            StoreId = null
        };
        context.Users.Add(sysadmin);

        // ================= TOKO A =================

        // Seed Owner User Toko A
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Name = "Pemilik Toko A",
            Username = "owner",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner123!"),
            Role = UserRole.Owner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            StoreId = storeAId
        };
        context.Users.Add(owner);

        // Seed Default Admin User Toko A
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin Utama A",
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Id,
            StoreId = storeAId
        };
        context.Users.Add(admin);

        // Seed Default Store Profile Toko A
        var storeProfile = new StoreProfile
        {
            Id = storeAId,
            Name = "Toko Kelontong A",
            Address = "Jl. Contoh No. 1, Kelurahan, Kota",
            Phone = "08123456789",
            Currency = "IDR",
            Timezone = "Asia/Jakarta",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Id
        };
        context.StoreProfiles.Add(storeProfile);

        // Seed Default Store Settings Toko A
        var storeSettings = new StoreSettings
        {
            Id = Guid.NewGuid(),
            StoreId = storeAId,
            AllowNegativeStock = false,
            RequireCashSessionForSales = true,
            DefaultLowStockThreshold = 10,
            EnableBarcode = true,
            EnablePurchasePriceTracking = true,
            DefaultPaymentMethod = PaymentMethod.Cash.ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Id
        };
        context.StoreSettings.Add(storeSettings);

        // Seed Default Units Toko A
        var units = new List<Unit>
        {
            new() { Id = Guid.NewGuid(), Name = "pcs", Description = "Satuan per buah/item", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "renceng", Description = "Satuan per renceng (10 pcs)", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "dus", Description = "Satuan per dus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "bungkus", Description = "Satuan per bungkus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "botol", Description = "Satuan per botol", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "kg", Description = "Kilogram", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "liter", Description = "Liter", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "gram", Description = "Gram", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId }
        };
        context.Units.AddRange(units);

        // Seed Default Categories Toko A
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Makanan Instan", Description = "Makanan siap saji dan instan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Minuman", Description = "Minuman kemasan dan siap saji", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Sembako", Description = "Bahan pokok dan kebutuhan sehari-hari", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Snack", Description = "Makanan ringan dan camilan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Sabun & Sampo", Description = "Perlengkapan mandi dan kebersihan diri", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Obat-Obatan", Description = "Obat-obatan dan vitamin", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Rokok", Description = "Rokok dan tembakau", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Lain-Lain", Description = "Barang lainnya", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId }
        };
        context.Categories.AddRange(categories);

        // Seed Default Expense Categories Toko A
        var expenseCategories = new List<ExpenseCategory>
        {
            new() { Id = Guid.NewGuid(), Name = "Listrik", Description = "Biaya listrik toko", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Air", Description = "Biaya air toko", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Internet", Description = "Biaya internet/wifi", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Sewa Toko", Description = "Biaya sewa tempat", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Gaji Pegawai", Description = "Biaya gaji karyawan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Transportasi", Description = "Biaya transportasi/olaraga", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Plastik/Kemasan", Description = "Biaya plastik dan kemasan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Name = "Lain-Lain", Description = "Biaya lain-lain", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId }
        };
        context.ExpenseCategories.AddRange(expenseCategories);

        // Seed Default Suppliers Toko A
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "PT. Sinar Jaya Distribusi",
            ContactPerson = "Budi Hartono",
            Phone = "08123456701",
            Email = "sinarjaya@mail.com",
            Address = "Jl. Industri No. 10, Jakarta",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Id,
            StoreId = storeAId
        };
        context.Suppliers.Add(supplier);

        // Seed Default Customers Toko A
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Budi Santoso (Pelanggan Tetap)",
            Phone = "08567890123",
            Address = "Jl. Melati No. 5, RT 02/03",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Id,
            StoreId = storeAId
        };
        context.Customers.Add(customer);

        // Retrieve seeded categories and units for product references
        var catMakanan = categories.First(c => c.Name == "Makanan Instan");
        var catMinuman = categories.First(c => c.Name == "Minuman");
        var catSembako = categories.First(c => c.Name == "Sembako");
        var catSnack = categories.First(c => c.Name == "Snack");
        
        var unitPcs = units.First(u => u.Name == "pcs");
        var unitBungkus = units.First(u => u.Name == "bungkus");
        var unitBotol = units.First(u => u.Name == "botol");
        var unitKg = units.First(u => u.Name == "kg");

        // Seed Default Products Toko A
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Sku = "P001", Barcode = "8998866200225", Name = "Indomie Goreng Spesial", CategoryId = catMakanan.Id, UnitId = unitBungkus.Id, PurchasePrice = 2800, SellingPrice = 3500, CurrentStock = 120, LowStockThreshold = 10, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Sku = "P002", Barcode = "8999999056472", Name = "Aqua Botol 600ml", CategoryId = catMinuman.Id, UnitId = unitBotol.Id, PurchasePrice = 2200, SellingPrice = 3000, CurrentStock = 45, LowStockThreshold = 10, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Sku = "P003", Barcode = "8992761001018", Name = "Bimoli Minyak Goreng 1L", CategoryId = catSembako.Id, UnitId = unitPcs.Id, PurchasePrice = 16500, SellingPrice = 18500, CurrentStock = 4, LowStockThreshold = 5, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Sku = "P004", Barcode = "8996001300226", Name = "Chitato Rasa Sapi Panggang 68g", CategoryId = catSnack.Id, UnitId = unitBungkus.Id, PurchasePrice = 8500, SellingPrice = 10500, CurrentStock = 0, LowStockThreshold = 5, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId },
            new() { Id = Guid.NewGuid(), Sku = "P005", Barcode = "8992689000102", Name = "Gula Pasir Putih 1kg", CategoryId = catSembako.Id, UnitId = unitKg.Id, PurchasePrice = 13500, SellingPrice = 15000, CurrentStock = 30, LowStockThreshold = 5, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = owner.Id, StoreId = storeAId }
        };
        context.Products.AddRange(products);

        // Add Initial Stock Movements for these products
        foreach (var p in products)
        {
            if (p.CurrentStock > 0)
            {
                var sm = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeAId,
                    ProductId = p.Id,
                    MovementDate = DateTime.UtcNow,
                    MovementType = StockMovementType.OpeningStock,
                    QuantityBefore = 0,
                    QuantityChange = p.CurrentStock,
                    QuantityAfter = p.CurrentStock,
                    Notes = "Saldo awal database Toko A",
                    CreatedBy = owner.Id
                };
                context.StockMovements.Add(sm);
            }
        }


        // ================= TOKO B =================

        // Seed Owner User Toko B
        var ownerB = new User
        {
            Id = Guid.NewGuid(),
            Name = "Pemilik Toko B",
            Username = "ownerb",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner123!"),
            Role = UserRole.Owner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            StoreId = storeBId
        };
        context.Users.Add(ownerB);

        // Seed Default Admin User Toko B
        var adminB = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin Utama B",
            Username = "adminb",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ownerB.Id,
            StoreId = storeBId
        };
        context.Users.Add(adminB);

        // Seed Default Store Profile Toko B
        var storeProfileB = new StoreProfile
        {
            Id = storeBId,
            Name = "Toko Kelontong B",
            Address = "Jl. Sukabumi No. 99, Bandung",
            Phone = "08987654321",
            Currency = "IDR",
            Timezone = "Asia/Jakarta",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ownerB.Id
        };
        context.StoreProfiles.Add(storeProfileB);

        // Seed Default Store Settings Toko B
        var storeSettingsB = new StoreSettings
        {
            Id = Guid.NewGuid(),
            StoreId = storeBId,
            AllowNegativeStock = false,
            RequireCashSessionForSales = true,
            DefaultLowStockThreshold = 5,
            EnableBarcode = true,
            EnablePurchasePriceTracking = true,
            DefaultPaymentMethod = PaymentMethod.Cash.ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ownerB.Id
        };
        context.StoreSettings.Add(storeSettingsB);

        // Seed Default Units Toko B
        var unitsB = new List<Unit>
        {
            new() { Id = Guid.NewGuid(), Name = "pcs", Description = "Satuan per buah/item", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId },
            new() { Id = Guid.NewGuid(), Name = "dus", Description = "Satuan per dus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId }
        };
        context.Units.AddRange(unitsB);

        // Seed Default Categories Toko B
        var categoriesB = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Makanan Ringan", Description = "Camilan dan Snack", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId },
            new() { Id = Guid.NewGuid(), Name = "Bahan Pokok", Description = "Beras, Minyak, Telur", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId }
        };
        context.Categories.AddRange(categoriesB);

        // Seed Default Products Toko B
        var productsB = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Sku = "PB01", Barcode = "8990000000001", Name = "Beras Pandan Wangi 5kg", CategoryId = categoriesB[1].Id, UnitId = unitsB[0].Id, PurchasePrice = 65000, SellingPrice = 72000, CurrentStock = 20, LowStockThreshold = 3, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId },
            new() { Id = Guid.NewGuid(), Sku = "PB02", Barcode = "8990000000002", Name = "Kripik Singkong Pedas B", CategoryId = categoriesB[0].Id, UnitId = unitsB[0].Id, PurchasePrice = 8000, SellingPrice = 10000, CurrentStock = 15, LowStockThreshold = 3, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerB.Id, StoreId = storeBId }
        };
        context.Products.AddRange(productsB);

        // Seed Stock Movements Toko B
        foreach (var p in productsB)
        {
            if (p.CurrentStock > 0)
            {
                var sm = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeBId,
                    ProductId = p.Id,
                    MovementDate = DateTime.UtcNow,
                    MovementType = StockMovementType.OpeningStock,
                    QuantityBefore = 0,
                    QuantityChange = p.CurrentStock,
                    QuantityAfter = p.CurrentStock,
                    Notes = "Saldo awal database Toko B",
                    CreatedBy = ownerB.Id
                };
                context.StockMovements.Add(sm);
            }
        }

        await context.SaveChangesAsync();
    }
}
