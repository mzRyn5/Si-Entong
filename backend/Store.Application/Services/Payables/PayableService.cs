using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.Payables;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.Payables;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;
using Store.SharedKernel;

namespace Store.Application.Services.Payables;

public interface IPayableService
{
    Task<PagedResponse<PayableListItemResponse>> GetAllAsync(
        Guid? supplierId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PayableResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayableResponse> RecordPaymentAsync(
        Guid id,
        RecordPayablePaymentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PayableResponse> CancelPaymentAsync(
        Guid payableId,
        Guid paymentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class PayableService : IPayableService
{
    private readonly IPayableRepository _payableRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public PayableService(
        IPayableRepository payableRepository,
        IAuditLogRepository auditLogRepository)
    {
        _payableRepository = payableRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<PayableListItemResponse>> GetAllAsync(
        Guid? supplierId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var payables = await _payableRepository.GetAllAsync(supplierId, status, page, pageSize, cancellationToken);
        var totalCount = await _payableRepository.GetTotalCountAsync(supplierId, status, cancellationToken);

        var responses = payables.Select(p => new PayableListItemResponse
        {
            Id = p.Id,
            PayableNumber = p.PayableNumber,
            PurchaseNumber = p.Purchase?.PurchaseNumber ?? string.Empty,
            SupplierName = p.Supplier?.Name ?? string.Empty,
            TotalAmount = p.TotalAmount,
            PaidAmount = p.PaidAmount,
            RemainingAmount = p.RemainingAmount,
            DueDate = p.DueDate,
            PaymentStatus = p.PaymentStatus.ToString(),
            CreatedAt = p.CreatedAt
        }).ToList();

        return new PagedResponse<PayableListItemResponse>
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

    public async Task<PayableResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payable = await _payableRepository.GetByIdAsync(id, cancellationToken);
        if (payable == null) return null;

        return MapToResponse(payable);
    }

    public async Task<PayableResponse> RecordPaymentAsync(
        Guid id,
        RecordPayablePaymentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var payable = await _payableRepository.GetByIdAsync(id, cancellationToken);
        if (payable == null)
        {
            throw new NotFoundException("Payable", id.ToString());
        }

        if (payable.PaymentStatus == PaymentStatus.Paid)
        {
            throw new BusinessRuleException("Hutang ini sudah lunas.");
        }

        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Nominal pembayaran harus lebih besar dari 0.");
        }

        if (request.Amount > payable.RemainingAmount)
        {
            throw new BusinessRuleException("Nominal pembayaran tidak boleh melebihi sisa hutang.");
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            paymentMethod = PaymentMethod.Cash;
        }

        // Create payment entry
        var payment = new PayablePayment
        {
            PayableId = payable.Id,
            PaymentDate = request.PaymentDate.ToUniversalTime(),
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            Notes = request.Notes,
            CreatedBy = userId
        };

        payable.Payments.Add(payment);

        // Update payable status and amounts
        payable.PaidAmount += request.Amount;
        payable.RemainingAmount = payable.TotalAmount - payable.PaidAmount;
        
        if (payable.RemainingAmount == 0)
        {
            payable.PaymentStatus = PaymentStatus.Paid;
        }
        else
        {
            payable.PaymentStatus = PaymentStatus.Partial;
        }

        payable.UpdatedBy = userId;

        // Save
        await _payableRepository.AddPaymentAsync(payment, cancellationToken);
        await _payableRepository.UpdateAsync(payable, cancellationToken);

        // Audit Log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "RecordPayablePayment",
            EntityName = "Payable",
            EntityId = payable.Id,
            Module = "Payables"
        }, cancellationToken);

        // Fetch again to include the new payment in the navigation collection
        var updatedPayable = await _payableRepository.GetByIdAsync(payable.Id, cancellationToken);
        return MapToResponse(updatedPayable ?? payable);
    }

    public async Task<PayableResponse> CancelPaymentAsync(
        Guid payableId,
        Guid paymentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var payable = await _payableRepository.GetByIdAsync(payableId, cancellationToken);
        if (payable == null)
        {
            throw new NotFoundException("Payable", payableId.ToString());
        }

        var payment = payable.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
        {
            throw new NotFoundException("PayablePayment", paymentId.ToString());
        }

        // Update amounts
        payable.PaidAmount -= payment.Amount;
        if (payable.PaidAmount < 0) payable.PaidAmount = 0;
        payable.RemainingAmount = payable.TotalAmount - payable.PaidAmount;

        // Update status
        if (payable.RemainingAmount == 0)
        {
            payable.PaymentStatus = PaymentStatus.Paid;
        }
        else if (payable.PaidAmount > 0)
        {
            payable.PaymentStatus = PaymentStatus.Partial;
        }
        else
        {
            payable.PaymentStatus = PaymentStatus.Unpaid;
        }

        payable.UpdatedBy = userId;

        // Remove payment
        await _payableRepository.RemovePaymentAsync(payment, cancellationToken);
        await _payableRepository.UpdateAsync(payable, cancellationToken);

        // Audit Log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "CancelPayablePayment",
            EntityName = "Payable",
            EntityId = payable.Id,
            Module = "Payables"
        }, cancellationToken);

        // Fetch again to include the updated payments in the navigation collection
        var updatedPayable = await _payableRepository.GetByIdAsync(payable.Id, cancellationToken);
        return MapToResponse(updatedPayable ?? payable);
    }

    private static PayableResponse MapToResponse(Payable payable) => new()
    {
        Id = payable.Id,
        PayableNumber = payable.PayableNumber,
        PurchaseId = payable.PurchaseId,
        PurchaseNumber = payable.Purchase?.PurchaseNumber ?? string.Empty,
        SupplierId = payable.SupplierId,
        SupplierName = payable.Supplier?.Name ?? string.Empty,
        TotalAmount = payable.TotalAmount,
        PaidAmount = payable.PaidAmount,
        RemainingAmount = payable.RemainingAmount,
        DueDate = payable.DueDate,
        PaymentStatus = payable.PaymentStatus.ToString(),
        Notes = payable.Notes,
        CreatedAt = payable.CreatedAt,
        Payments = payable.Payments.Select(pp => new PayablePaymentResponse
        {
            Id = pp.Id,
            PayableId = pp.PayableId,
            PaymentDate = pp.PaymentDate,
            Amount = pp.Amount,
            PaymentMethod = pp.PaymentMethod.ToString(),
            Notes = pp.Notes
        }).ToList()
    };

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var payable = await _payableRepository.GetByIdAsync(id, cancellationToken);
        if (payable == null) return false;

        await _payableRepository.DeleteAsync(payable, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "Delete",
            EntityName = "Payable",
            EntityId = payable.Id,
            Module = "Payables",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
