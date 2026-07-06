using Store.Domain.Entities;
using Store.Domain.Enums;

namespace Store.Application.Abstractions.Repositories;

public interface IPayableRepository
{
    Task<Payable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payable>> GetAllAsync(Guid? supplierId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? supplierId, string? status, CancellationToken cancellationToken = default);
    Task<Payable> AddAsync(Payable payable, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payable payable, CancellationToken cancellationToken = default);
    Task DeleteAsync(Payable payable, CancellationToken cancellationToken = default);
    Task<string> GetLastPayableNumberAsync(string prefix, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(PayablePayment payment, CancellationToken cancellationToken = default);
    Task RemovePaymentAsync(PayablePayment payment, CancellationToken cancellationToken = default);
}
