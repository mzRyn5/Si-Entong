using Store.Contracts.Requests.Suppliers;
using Store.Contracts.Requests.Customers;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Suppliers;
using Store.Contracts.Responses.Customers;
using Store.Domain.Entities;
using Store.Domain.Exceptions;
using Store.Application.Abstractions.Repositories;

namespace Store.Application.Services.MasterData;

public interface ISupplierService
{
    Task<PagedResponse<SupplierResponse>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
}

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public SupplierService(ISupplierRepository supplierRepository, IAuditLogRepository auditLogRepository)
    {
        _supplierRepository = supplierRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<SupplierResponse>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        var totalCount = await _supplierRepository.GetTotalCountAsync(search, cancellationToken);

        var responses = suppliers.Select(MapToResponse).ToList();

        return new PagedResponse<SupplierResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        return supplier == null ? null : MapToResponse(supplier);
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _supplierRepository.AddAsync(supplier, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Supplier",
            EntityId = created.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null) return null;

        supplier.Name = request.Name;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.Notes = request.Notes;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedBy = updatedBy;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "Supplier",
            EntityId = supplier.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(supplier);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null) return false;

        if (await _supplierRepository.HasActivePurchasesAsync(id, cancellationToken))
        {
            throw new BusinessRuleException("Supplier masih memiliki transaksi pembelian aktif.", "HAS_ACTIVE_PURCHASES");
        }

        supplier.IsActive = false;
        supplier.UpdatedBy = deletedBy;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Deactivate",
            EntityName = "Supplier",
            EntityId = supplier.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static SupplierResponse MapToResponse(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Phone = supplier.Phone,
        Address = supplier.Address,
        Notes = supplier.Notes,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt
    };
}

public interface ICustomerService
{
    Task<PagedResponse<CustomerResponse>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CustomerService(ICustomerRepository customerRepository, IAuditLogRepository auditLogRepository)
    {
        _customerRepository = customerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<CustomerResponse>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(search, isActive, page, pageSize, cancellationToken);
        var totalCount = await _customerRepository.GetTotalCountAsync(search, cancellationToken);

        var responses = customers.Select(MapToResponse).ToList();

        return new PagedResponse<CustomerResponse>
        {
            Data = responses,
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _customerRepository.AddAsync(customer, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "Customer",
            EntityId = created.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null) return null;

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.Notes = request.Notes;
        customer.IsActive = request.IsActive;
        customer.UpdatedBy = updatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = updatedBy,
            Action = "Update",
            EntityName = "Customer",
            EntityId = customer.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(customer);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null) return false;

        customer.IsActive = false;
        customer.UpdatedBy = deletedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = deletedBy,
            Action = "Deactivate",
            EntityName = "Customer",
            EntityId = customer.Id,
            Module = "MasterData",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    private static CustomerResponse MapToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Phone = customer.Phone,
        Address = customer.Address,
        Notes = customer.Notes,
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt
    };
}
