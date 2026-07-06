using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Application.Services.Users;
using Store.Contracts.Requests.Users;
using Store.Contracts.Responses.Common;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/sysadmin/stores")]
[Authorize(Policy = "SysAdminOnly")]
public class SysAdminStoresController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;

    public SysAdminStoresController(AppDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    /// <summary>
    /// Get all stores (Sys Admin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StoreProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var stores = await _context.StoreProfiles
            .IgnoreQueryFilters()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var owners = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Owner)
            .ToListAsync(cancellationToken);

        var dtos = stores.Select(s => {
            var owner = owners.FirstOrDefault(o => o.StoreId == s.Id);
            return new StoreProfileDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Phone = s.Phone,
                Currency = s.Currency,
                Timezone = s.Timezone,
                LogoUrl = s.LogoUrl,
                ReceiptFooter = s.ReceiptFooter,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                OwnerName = owner?.Name ?? "Belum ada owner"
            };
        });

        return SuccessResponse(dtos, "Daftar toko berhasil diambil.");
    }

    /// <summary>
    /// Get store by ID (Sys Admin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StoreProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var store = await _context.StoreProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (store == null)
        {
            return NotFoundResponse("Toko", id);
        }

        var owner = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StoreId == id && u.Role == UserRole.Owner, cancellationToken);

        var dto = new StoreProfileDto
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Phone = store.Phone,
            Currency = store.Currency,
            Timezone = store.Timezone,
            LogoUrl = store.LogoUrl,
            ReceiptFooter = store.ReceiptFooter,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt,
            OwnerName = owner?.Name ?? "Belum ada owner"
        };

        return SuccessResponse(dto, "Profil toko berhasil diambil.");
    }

    /// <summary>
    /// Create new store and its initial Owner account (Sys Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StoreProfileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        var usernameExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == request.OwnerUsername, cancellationToken);

        if (usernameExists)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Message = "Username owner sudah digunakan.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var storeId = Guid.NewGuid();
        var storeProfile = new StoreProfile
        {
            Id = storeId,
            Name = request.Name,
            Address = request.Address ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            Currency = request.Currency ?? "IDR",
            Timezone = request.Timezone ?? "Asia/Jakarta",
            LogoUrl = request.LogoUrl,
            ReceiptFooter = request.ReceiptFooter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.StoreProfiles.Add(storeProfile);

        var storeSettings = new StoreSettings
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            AllowNegativeStock = false,
            RequireCashSessionForSales = true,
            DefaultLowStockThreshold = 5,
            EnableBarcode = true,
            EnablePurchasePriceTracking = true,
            DefaultPaymentMethod = PaymentMethod.Cash.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        _context.StoreSettings.Add(storeSettings);

        var ownerUser = new User
        {
            Id = Guid.NewGuid(),
            Name = request.OwnerName,
            Username = request.OwnerUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword),
            Role = UserRole.Owner,
            IsActive = true,
            StoreId = storeId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(ownerUser);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new StoreProfileDto
        {
            Id = storeProfile.Id,
            Name = storeProfile.Name,
            Address = storeProfile.Address,
            Phone = storeProfile.Phone,
            Currency = storeProfile.Currency,
            Timezone = storeProfile.Timezone,
            LogoUrl = storeProfile.LogoUrl,
            ReceiptFooter = storeProfile.ReceiptFooter,
            IsActive = storeProfile.IsActive,
            CreatedAt = storeProfile.CreatedAt,
            OwnerName = request.OwnerName
        };

        return CreatedResponse(dto, "Toko baru berhasil didaftarkan.");
    }

    /// <summary>
    /// Update store profile (Sys Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StoreProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSysStoreProfileRequest request, CancellationToken cancellationToken)
    {
        var store = await _context.StoreProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (store == null)
        {
            return NotFoundResponse("Toko", id);
        }

        store.Name = request.Name;
        store.Address = request.Address ?? string.Empty;
        store.Phone = request.Phone ?? string.Empty;
        store.Currency = request.Currency ?? "IDR";
        store.Timezone = request.Timezone ?? "Asia/Jakarta";
        store.LogoUrl = request.LogoUrl;
        store.ReceiptFooter = request.ReceiptFooter;

        _context.StoreProfiles.Update(store);
        await _context.SaveChangesAsync(cancellationToken);

        var owner = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StoreId == id && u.Role == UserRole.Owner, cancellationToken);

        var dto = new StoreProfileDto
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Phone = store.Phone,
            Currency = store.Currency,
            Timezone = store.Timezone,
            LogoUrl = store.LogoUrl,
            ReceiptFooter = store.ReceiptFooter,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt,
            OwnerName = owner?.Name ?? "Belum ada owner"
        };

        return SuccessResponse(dto, "Profil toko berhasil diperbarui.");
    }

    /// <summary>
    /// Toggle store active status (Sys Admin only)
    /// </summary>
    [HttpPost("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<StoreProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var store = await _context.StoreProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (store == null)
        {
            return NotFoundResponse("Toko", id);
        }

        store.IsActive = !store.IsActive;

        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.StoreId == id)
            .ToListAsync(cancellationToken);

        foreach (var u in users)
        {
            u.IsActive = store.IsActive;
        }

        _context.StoreProfiles.Update(store);
        await _context.SaveChangesAsync(cancellationToken);

        var owner = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StoreId == id && u.Role == UserRole.Owner, cancellationToken);

        var dto = new StoreProfileDto
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Phone = store.Phone,
            Currency = store.Currency,
            Timezone = store.Timezone,
            LogoUrl = store.LogoUrl,
            ReceiptFooter = store.ReceiptFooter,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt,
            OwnerName = owner?.Name ?? "Belum ada owner"
        };

        var statusText = store.IsActive ? "diaktifkan" : "dinonaktifkan";
        return SuccessResponse(dto, $"Toko berhasil {statusText}.");
    }
}

public class StoreProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Currency { get; set; } = "IDR";
    public string Timezone { get; set; } = "Asia/Jakarta";
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}

public class CreateStoreRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }

    [Required]
    public string OwnerName { get; set; } = string.Empty;
    [Required]
    public string OwnerUsername { get; set; } = string.Empty;
    [Required]
    public string OwnerPassword { get; set; } = string.Empty;
}

public class UpdateSysStoreProfileRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
}
