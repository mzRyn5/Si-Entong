using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.CashSessions;
using Store.Contracts.Responses.CashSessions;
using Store.Contracts.Responses.Common;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;

namespace Store.Application.Services.Sales;

public interface ICashSessionService
{
    Task<PagedResponse<CashSessionListItemResponse>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CashSessionResponse?> GetActiveAsync(Guid cashierId, CancellationToken cancellationToken = default);

    Task<CashSessionResponse> OpenAsync(
        OpenCashSessionRequest request,
        Guid cashierId,
        CancellationToken cancellationToken = default);

    Task<CashSessionResponse?> CloseAsync(
        Guid id,
        CloseCashSessionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class CashSessionService : ICashSessionService
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CashSessionService(
        ICashSessionRepository cashSessionRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository)
    {
        _cashSessionRepository = cashSessionRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<CashSessionListItemResponse>> GetAllAsync(
        Guid? cashierId,
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _cashSessionRepository.GetAllAsync(
            cashierId, fromDate, toDate, status, page, pageSize, cancellationToken);
        var totalCount = await _cashSessionRepository.GetTotalCountAsync(
            cashierId, fromDate, toDate, status, cancellationToken);

        return new PagedResponse<CashSessionListItemResponse>
        {
            Data = sessions.Select(s => new CashSessionListItemResponse
            {
                Id = s.Id,
                CashierName = s.Cashier?.Name ?? string.Empty,
                OpenedAt = s.OpenedAt,
                OpeningCashAmount = s.OpeningCashAmount,
                Status = s.Status.ToString()
            }).ToList(),
            Meta = new MetaData
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };
    }

    public async Task<CashSessionResponse?> GetActiveAsync(
        Guid cashierId,
        CancellationToken cancellationToken = default)
    {
        var session = await _cashSessionRepository.GetActiveByCashierIdAsync(cashierId, cancellationToken);
        return session == null ? null : MapToResponse(session);
    }

    public async Task<CashSessionResponse> OpenAsync(
        OpenCashSessionRequest request,
        Guid cashierId,
        CancellationToken cancellationToken = default)
    {
        var cashier = await _userRepository.GetByIdAsync(cashierId, cancellationToken);
        if (cashier == null || !cashier.IsActive)
        {
            throw new BusinessRuleException("User kasir tidak aktif atau tidak ditemukan.");
        }

        var activeSession = await _cashSessionRepository.GetActiveByCashierIdAsync(cashierId, cancellationToken);
        if (activeSession != null)
        {
            throw new BusinessRuleException("Kasir masih memiliki sesi kasir aktif.");
        }

        if (request.OpeningCashAmount < 0)
        {
            throw new BusinessRuleException("Modal kas awal tidak boleh negatif.");
        }

        var session = new CashSession
        {
            CashierId = cashierId,
            OpenedAt = request.OpenedAt == default ? DateTime.UtcNow : request.OpenedAt,
            OpeningCashAmount = request.OpeningCashAmount,
            CashSalesAmount = 0,
            CashInAmount = 0,
            CashOutAmount = 0,
            Status = CashSessionStatus.Open,
            CreatedBy = cashierId
        };

        var created = await _cashSessionRepository.AddAsync(session, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = cashierId,
            Action = "OpenCashSession",
            EntityName = "CashSession",
            EntityId = created.Id,
            Module = "CashSessions",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var result = await _cashSessionRepository.GetByIdAsync(created.Id, cancellationToken);
        return MapToResponse(result ?? created);
    }

    public async Task<CashSessionResponse?> CloseAsync(
        Guid id,
        CloseCashSessionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _cashSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null) return null;

        if (session.Status != CashSessionStatus.Open)
        {
            throw new BusinessRuleException("Hanya sesi kasir Open yang dapat ditutup.");
        }

        if (request.ActualCashAmount < 0)
        {
            throw new BusinessRuleException("Jumlah kas aktual tidak boleh negatif.");
        }

        session.ClosedAt = request.ClosedAt == default ? DateTime.UtcNow : request.ClosedAt;
        session.ActualCashAmount = request.ActualCashAmount;
        session.ClosingNotes = request.Notes;
        session.ClosedBy = userId;
        session.UpdatedBy = userId;
        session.Status = CashSessionStatus.Closed;

        await _cashSessionRepository.UpdateAsync(session, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "CloseCashSession",
            EntityName = "CashSession",
            EntityId = session.Id,
            Module = "CashSessions",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(session);
    }

    private static CashSessionResponse MapToResponse(CashSession session) => new()
    {
        Id = session.Id,
        CashierId = session.CashierId,
        CashierName = session.Cashier?.Name ?? string.Empty,
        OpenedAt = session.OpenedAt,
        OpeningCashAmount = session.OpeningCashAmount,
        CashSalesAmount = session.CashSalesAmount,
        CashInAmount = session.CashInAmount,
        CashOutAmount = session.CashOutAmount,
        ExpectedCashAmount = session.ExpectedCashAmount,
        ActualCashAmount = session.ActualCashAmount,
        DifferenceAmount = session.DifferenceAmount,
        ClosedAt = session.ClosedAt,
        Status = session.Status.ToString()
    };
}
