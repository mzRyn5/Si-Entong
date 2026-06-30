using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Users;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Users;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using BCrypt.Net;

namespace Store.Application.Services.Users;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetAllAsync(string? search, string? role, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(Guid id, string newPassword, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<UserResponse>> GetAllAsync(
        string? search,
        string? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(search, page, pageSize, cancellationToken);
        var total = await _repository.GetTotalCountAsync(search, cancellationToken);

        return new PagedResponse<UserResponse>
        {
            Data = users.Select(Map).ToList(),
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is not null ? Map(user) : null;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest r, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var role = Parse(r.Role);

        // Determine StoreId
        Guid? storeId = null;
        if (role != UserRole.SysAdmin)
        {
            if (r.StoreId.HasValue)
            {
                storeId = r.StoreId.Value;
            }
            else
            {
                var creator = await _repository.GetByIdAsync(currentUserId, cancellationToken);
                if (creator != null)
                {
                    storeId = creator.StoreId;
                }
            }
        }

        // Limit check for Admin (max 3 active)
        if (role == UserRole.Admin && r.IsActive)
        {
            var allUsers = await _repository.GetAllAsync(cancellationToken);
            var activeAdminCount = allUsers.Count(u => u.Role == UserRole.Admin && u.IsActive && u.StoreId == storeId);
            if (activeAdminCount >= 3)
            {
                throw new BusinessRuleException("Batas maksimal admin penjaga toko (3 user) telah tercapai.", "ADMIN_LIMIT_EXCEEDED");
            }
        }

        var user = new User
        {
            Name = r.Name,
            Username = r.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(r.Password),
            Role = role,
            IsActive = r.IsActive,
            CreatedBy = currentUserId,
            StoreId = storeId
        };

        await _repository.AddAsync(user, cancellationToken);
        return Map(user);
    }

    public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest r, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null) return null;

        var newRole = Parse(r.Role);
        Guid? storeId = null;
        if (newRole != UserRole.SysAdmin)
        {
            storeId = r.StoreId ?? user.StoreId;
        }

        // Limit check for Admin (max 3 active)
        if (newRole == UserRole.Admin && r.IsActive && (!user.IsActive || user.Role != UserRole.Admin || user.StoreId != storeId))
        {
            var allUsers = await _repository.GetAllAsync(cancellationToken);
            var activeAdminCount = allUsers.Count(u => u.Role == UserRole.Admin && u.IsActive && u.StoreId == storeId && u.Id != id);
            if (activeAdminCount >= 3)
            {
                throw new BusinessRuleException("Batas maksimal admin penjaga toko (3 user) telah tercapai.", "ADMIN_LIMIT_EXCEEDED");
            }
        }

        user.Name = r.Name;
        user.Role = newRole;
        user.IsActive = r.IsActive;
        user.StoreId = storeId;
        user.UpdatedBy = currentUserId;

        await _repository.UpdateAsync(user, cancellationToken);
        return Map(user);
    }

    public async Task<bool> ResetPasswordAsync(Guid id, string p, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(p);
        user.UpdatedBy = currentUserId;

        await _repository.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("Password lama yang Anda masukkan salah.", "INVALID_PASSWORD");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedBy = userId;

        await _repository.UpdateAsync(user, cancellationToken);
        return true;
    }

    private static UserRole Parse(string r)
    {
        if (r.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)) return UserRole.SysAdmin;
        if (r.Equals("owner", StringComparison.OrdinalIgnoreCase)) return UserRole.Owner;
        return UserRole.Admin;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null) return false;

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static UserResponse Map(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Username = u.Username,
        Role = u.Role.ToString().ToLowerInvariant(),
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        StoreId = u.StoreId
    };
}
