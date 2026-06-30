using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Receivables;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Receivables;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using Store.SharedKernel;

namespace Store.Application.Services.Receivables;

public interface IReceivableService
{
    Task<PagedResponse<ReceivableListItemResponse>> GetAllAsync(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReceivableResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ReceivableResponse> RecordPaymentAsync(
        Guid id,
        RecordReceivablePaymentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class ReceivableService : IReceivableService
{
    private readonly IReceivableRepository _receivableRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ReceivableService(
        IReceivableRepository receivableRepository,
        IAuditLogRepository auditLogRepository)
    {
        _receivableRepository = receivableRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<ReceivableListItemResponse>> GetAllAsync(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var receivables = await _receivableRepository.GetAllAsync(customerId, status, page, pageSize, cancellationToken);
        var totalCount = await _receivableRepository.GetTotalCountAsync(customerId, status, cancellationToken);

        var responses = receivables.Select(r => new ReceivableListItemResponse
        {
            Id = r.Id,
            ReceivableNumber = r.ReceivableNumber,
            SaleNumber = r.Sale?.SaleNumber ?? string.Empty,
            CustomerName = r.Customer?.Name ?? string.Empty,
            TotalAmount = r.TotalAmount,
            PaidAmount = r.PaidAmount,
            RemainingAmount = r.RemainingAmount,
            DueDate = r.DueDate,
            PaymentStatus = r.PaymentStatus.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

        return new PagedResponse<ReceivableListItemResponse>
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

    public async Task<ReceivableResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var receivable = await _receivableRepository.GetByIdAsync(id, cancellationToken);
        if (receivable == null) return null;

        return MapToResponse(receivable);
    }

    public async Task<ReceivableResponse> RecordPaymentAsync(
        Guid id,
        RecordReceivablePaymentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var receivable = await _receivableRepository.GetByIdAsync(id, cancellationToken);
        if (receivable == null)
        {
            throw new NotFoundException("Receivable", id.ToString());
        }

        if (receivable.PaymentStatus == PaymentStatus.Paid)
        {
            throw new BusinessRuleException("Piutang ini sudah lunas.");
        }

        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Nominal pembayaran harus lebih besar dari 0.");
        }

        if (request.Amount > receivable.RemainingAmount)
        {
            throw new BusinessRuleException("Nominal pembayaran tidak boleh melebihi sisa piutang.");
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            paymentMethod = PaymentMethod.Cash;
        }

        // Create payment entry
        var payment = new ReceivablePayment
        {
            ReceivableId = receivable.Id,
            PaymentDate = request.PaymentDate.ToUniversalTime(),
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            Notes = request.Notes,
            CreatedBy = userId
        };

        receivable.Payments.Add(payment);

        // Update receivable status and amounts
        receivable.PaidAmount += request.Amount;
        receivable.RemainingAmount = receivable.TotalAmount - receivable.PaidAmount;

        if (receivable.RemainingAmount == 0)
        {
            receivable.PaymentStatus = PaymentStatus.Paid;
        }
        else
        {
            receivable.PaymentStatus = PaymentStatus.Partial;
        }

        receivable.UpdatedBy = userId;

        // Save
        await _receivableRepository.AddPaymentAsync(payment, cancellationToken);
        await _receivableRepository.UpdateAsync(receivable, cancellationToken);

        // Audit Log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "RecordReceivablePayment",
            EntityName = "Receivable",
            EntityId = receivable.Id,
            Module = "Receivables"
        }, cancellationToken);

        // Fetch again to include the new payment in the navigation collection
        var updatedReceivable = await _receivableRepository.GetByIdAsync(receivable.Id, cancellationToken);
        return MapToResponse(updatedReceivable ?? receivable);
    }

    private static ReceivableResponse MapToResponse(Receivable receivable) => new()
    {
        Id = receivable.Id,
        ReceivableNumber = receivable.ReceivableNumber,
        SaleId = receivable.SaleId,
        SaleNumber = receivable.Sale?.SaleNumber ?? string.Empty,
        CustomerId = receivable.CustomerId,
        CustomerName = receivable.Customer?.Name ?? string.Empty,
        TotalAmount = receivable.TotalAmount,
        PaidAmount = receivable.PaidAmount,
        RemainingAmount = receivable.RemainingAmount,
        DueDate = receivable.DueDate,
        PaymentStatus = receivable.PaymentStatus.ToString(),
        Notes = receivable.Notes,
        CreatedAt = receivable.CreatedAt,
        Payments = receivable.Payments.Select(rp => new ReceivablePaymentResponse
        {
            Id = rp.Id,
            ReceivableId = rp.ReceivableId,
            PaymentDate = rp.PaymentDate,
            Amount = rp.Amount,
            PaymentMethod = rp.PaymentMethod.ToString(),
            Notes = rp.Notes
        }).ToList()
    };

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var receivable = await _receivableRepository.GetByIdAsync(id, cancellationToken);
        if (receivable == null) return false;

        await _receivableRepository.DeleteAsync(receivable, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Delete",
            EntityName = "Receivable",
            EntityId = receivable.Id,
            Module = "Receivables",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
