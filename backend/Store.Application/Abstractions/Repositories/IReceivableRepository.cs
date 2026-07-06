using Store.Domain.Entities;

namespace Store.Application.Abstractions.Repositories;

public interface IReceivableRepository
{
    Task<Receivable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Receivable>> GetAllAsync(Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? customerId, string? status, CancellationToken cancellationToken = default);
    Task<Receivable> AddAsync(Receivable receivable, CancellationToken cancellationToken = default);
    Task UpdateAsync(Receivable receivable, CancellationToken cancellationToken = default);
    Task DeleteAsync(Receivable receivable, CancellationToken cancellationToken = default);
    Task<string> GetLastReceivableNumberAsync(string prefix, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(ReceivablePayment payment, CancellationToken cancellationToken = default);
}
