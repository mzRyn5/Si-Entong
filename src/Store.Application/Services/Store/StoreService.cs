using Store.Application.Abstractions.Repositories;
using Store.Contracts.Responses.Common;
using Store.Domain.Entities;

namespace Store.Application.Services.Store;

public interface IStoreService
{
    Task<StoreProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<StoreProfileResponse?> UpdateProfileAsync(UpdateStoreProfileRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<StoreSettingsResponse?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<StoreSettingsResponse?> UpdateSettingsAsync(UpdateStoreSettingsRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
}

public class StoreProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Currency { get; set; } = "IDR";
    public string Timezone { get; set; } = "Asia/Jakarta";
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
}

public class StoreSettingsResponse
{
    public bool AllowNegativeStock { get; set; }
    public bool RequireCashSessionForSales { get; set; }
    public int DefaultLowStockThreshold { get; set; }
    public bool EnableBarcode { get; set; }
    public bool EnablePurchasePriceTracking { get; set; }
    public string DefaultPaymentMethod { get; set; } = "Cash";
}

public class UpdateStoreProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Currency { get; set; } = "IDR";
    public string Timezone { get; set; } = "Asia/Jakarta";
    public string? LogoUrl { get; set; }
    public string? ReceiptFooter { get; set; }
}

public class UpdateStoreSettingsRequest
{
    public bool AllowNegativeStock { get; set; }
    public bool RequireCashSessionForSales { get; set; }
    public int DefaultLowStockThreshold { get; set; }
    public bool EnableBarcode { get; set; }
    public bool EnablePurchasePriceTracking { get; set; }
    public string DefaultPaymentMethod { get; set; } = "Cash";
}

public class StoreService : IStoreService
{
    private readonly IStoreProfileRepository _profileRepository;
    private readonly IStoreSettingsRepository _settingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public StoreService(
        IStoreProfileRepository profileRepository,
        IStoreSettingsRepository settingsRepository,
        IAuditLogRepository auditLogRepository)
    {
        _profileRepository = profileRepository;
        _settingsRepository = settingsRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<StoreProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetAsync(cancellationToken);
        if (profile == null) return null;

        return new StoreProfileResponse
        {
            Id = profile.Id,
            Name = profile.Name,
            Address = profile.Address,
            Phone = profile.Phone,
            Currency = profile.Currency,
            Timezone = profile.Timezone,
            LogoUrl = profile.LogoUrl,
            ReceiptFooter = profile.ReceiptFooter
        };
    }

    public async Task<StoreProfileResponse?> UpdateProfileAsync(UpdateStoreProfileRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetAsync(cancellationToken);

        if (profile == null)
        {
            profile = new StoreProfile
            {
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Currency = request.Currency,
                Timezone = request.Timezone,
                LogoUrl = request.LogoUrl,
                ReceiptFooter = request.ReceiptFooter,
                CreatedBy = updatedBy,
                CreatedAt = DateTime.UtcNow
            };
            await _profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Name = request.Name;
            profile.Address = request.Address;
            profile.Phone = request.Phone;
            profile.Currency = request.Currency;
            profile.Timezone = request.Timezone;
            profile.LogoUrl = request.LogoUrl;
            profile.ReceiptFooter = request.ReceiptFooter;
            profile.UpdatedBy = updatedBy;
            profile.UpdatedAt = DateTime.UtcNow;
            await _profileRepository.UpdateAsync(profile, cancellationToken);
        }

        // Log audit
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "StoreProfile",
            EntityId = profile.Id,
            Module = "Store",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new StoreProfileResponse
        {
            Id = profile.Id,
            Name = profile.Name,
            Address = profile.Address,
            Phone = profile.Phone,
            Currency = profile.Currency,
            Timezone = profile.Timezone,
            LogoUrl = profile.LogoUrl,
            ReceiptFooter = profile.ReceiptFooter
        };
    }

    public async Task<StoreSettingsResponse?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings == null) return null;

        return new StoreSettingsResponse
        {
            AllowNegativeStock = settings.AllowNegativeStock,
            RequireCashSessionForSales = settings.RequireCashSessionForSales,
            DefaultLowStockThreshold = settings.DefaultLowStockThreshold,
            EnableBarcode = settings.EnableBarcode,
            EnablePurchasePriceTracking = settings.EnablePurchasePriceTracking,
            DefaultPaymentMethod = settings.DefaultPaymentMethod
        };
    }

    public async Task<StoreSettingsResponse?> UpdateSettingsAsync(UpdateStoreSettingsRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        if (settings == null)
        {
            settings = new StoreSettings
            {
                AllowNegativeStock = request.AllowNegativeStock,
                RequireCashSessionForSales = request.RequireCashSessionForSales,
                DefaultLowStockThreshold = request.DefaultLowStockThreshold,
                EnableBarcode = request.EnableBarcode,
                EnablePurchasePriceTracking = request.EnablePurchasePriceTracking,
                DefaultPaymentMethod = request.DefaultPaymentMethod,
                CreatedBy = updatedBy,
                CreatedAt = DateTime.UtcNow
            };
            await _settingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.AllowNegativeStock = request.AllowNegativeStock;
            settings.RequireCashSessionForSales = request.RequireCashSessionForSales;
            settings.DefaultLowStockThreshold = request.DefaultLowStockThreshold;
            settings.EnableBarcode = request.EnableBarcode;
            settings.EnablePurchasePriceTracking = request.EnablePurchasePriceTracking;
            settings.DefaultPaymentMethod = request.DefaultPaymentMethod;
            settings.UpdatedBy = updatedBy;
            settings.UpdatedAt = DateTime.UtcNow;
            await _settingsRepository.UpdateAsync(settings, cancellationToken);
        }

        // Log audit
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "StoreSettings",
            EntityId = settings.Id,
            Module = "Store",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new StoreSettingsResponse
        {
            AllowNegativeStock = settings.AllowNegativeStock,
            RequireCashSessionForSales = settings.RequireCashSessionForSales,
            DefaultLowStockThreshold = settings.DefaultLowStockThreshold,
            EnableBarcode = settings.EnableBarcode,
            EnablePurchasePriceTracking = settings.EnablePurchasePriceTracking,
            DefaultPaymentMethod = settings.DefaultPaymentMethod
        };
    }
}
