using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Infrastructure.Persistence;

public static class StoreSeeder
{
    public static async Task SeedStoreMasterDataAsync(AppDbContext context, Guid storeId, Guid ownerUserId, string storeName, CancellationToken cancellationToken = default)
    {
        // 1. Ensure Units
        var hasUnits = await context.Units.IgnoreQueryFilters().AnyAsync(u => u.StoreId == storeId, cancellationToken);
        if (!hasUnits)
        {
            var units = new List<Unit>
            {
                new() { Id = Guid.NewGuid(), Name = "pcs", Description = "Satuan per buah/item", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "renceng", Description = "Satuan per renceng (10 pcs)", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "dus", Description = "Satuan per dus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "bungkus", Description = "Satuan per bungkus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "botol", Description = "Satuan per botol", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "kg", Description = "Kilogram", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "liter", Description = "Liter", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "gram", Description = "Gram", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId }
            };
            context.Units.AddRange(units);
        }

        // 2. Ensure Categories
        var hasCategories = await context.Categories.IgnoreQueryFilters().AnyAsync(c => c.StoreId == storeId, cancellationToken);
        if (!hasCategories)
        {
            var categories = new List<Category>
            {
                new() { Id = Guid.NewGuid(), Name = "Makanan Instan", Description = "Makanan siap saji dan instan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Minuman", Description = "Minuman kemasan dan siap saji", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Sembako", Description = "Bahan pokok dan kebutuhan sehari-hari", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Snack", Description = "Makanan ringan dan camilan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Sabun & Sampo", Description = "Perlengkapan mandi dan kebersihan diri", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Obat-Obatan", Description = "Obat-obatan dan vitamin", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Rokok", Description = "Rokok dan tembakau", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Lain-Lain", Description = "Barang lainnya", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId }
            };
            context.Categories.AddRange(categories);
        }

        // 3. Ensure Expense Categories
        var hasExpenseCategories = await context.ExpenseCategories.IgnoreQueryFilters().AnyAsync(ec => ec.StoreId == storeId, cancellationToken);
        if (!hasExpenseCategories)
        {
            var expenseCategories = new List<ExpenseCategory>
            {
                new() { Id = Guid.NewGuid(), Name = "Listrik", Description = "Biaya listrik toko", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Air", Description = "Biaya air toko", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Internet", Description = "Biaya internet/wifi", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Sewa Toko", Description = "Biaya sewa tempat", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Gaji Pegawai", Description = "Biaya gaji karyawan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Transportasi", Description = "Biaya transportasi/olaraga", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Plastik/Kemasan", Description = "Biaya plastik dan kemasan", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId },
                new() { Id = Guid.NewGuid(), Name = "Lain-Lain", Description = "Biaya lain-lain", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = ownerUserId, StoreId = storeId }
            };
            context.ExpenseCategories.AddRange(expenseCategories);
        }

        // Save changes to write units, categories, and expense categories to DB
        await context.SaveChangesAsync(cancellationToken);
    }
}
