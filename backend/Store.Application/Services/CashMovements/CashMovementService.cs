using Store.Application.Abstractions.Repositories;
using Store.Contracts.Requests.CashMovements;
using Store.Contracts.Responses.Common;
using Store.Contracts.Responses.CashMovements;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.CashMovements;

public interface ICashMovementService
{
    Task<PagedResponse<CashMovementResponse>> GetAllMovementsAsync(Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CashMovementResponse> CreateMovementAsync(CreateCashMovementRequest request, Guid createdBy, CancellationToken cancellationToken = default);
}

public class CashMovementService : ICashMovementService
{
    private readonly ICashMovementRepository _cashMovementRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CashMovementService(
        ICashMovementRepository cashMovementRepository,
        ICashSessionRepository cashSessionRepository,
        IAuditLogRepository auditLogRepository)
    {
        _cashMovementRepository = cashMovementRepository;
        _cashSessionRepository = cashSessionRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<CashMovementResponse>> GetAllMovementsAsync(
        Guid? cashSessionId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var movements = await _cashMovementRepository.GetAllAsync(cashSessionId, type, fromDate, toDate, page, pageSize, cancellationToken);
        var totalCount = await _cashMovementRepository.GetTotalCountAsync(cashSessionId, type, fromDate, toDate, cancellationToken);

        var responses = movements.Select(MapToResponse).ToList();

        return new PagedResponse<CashMovementResponse>
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

    public async Task<CashMovementResponse> CreateMovementAsync(
        CreateCashMovementRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Jumlah mutasi harus lebih besar dari 0.", "INVALID_AMOUNT");
        }

        if (request.Type != Domain.Enums.CashMovementType.In && request.Type != Domain.Enums.CashMovementType.Out)
        {
            throw new BusinessRuleException("Tipe mutasi kas harus berupa In atau Out.", "INVALID_MOVEMENT_TYPE");
        }

        var activeSession = await _cashSessionRepository.GetActiveByCashierIdAsync(createdBy, cancellationToken);
        if (activeSession == null)
        {
            throw new BusinessRuleException("Tidak ada sesi kasir aktif yang sedang berjalan untuk Anda.", "NO_ACTIVE_SESSION");
        }

        var movement = new CashMovement
        {
            CashSessionId = activeSession.Id,
            MovementDate = DateTime.UtcNow,
            MovementType = request.Type,
            Amount = request.Amount,
            Notes = request.Description,
            Category = request.Type.ToString(),
            Status = Domain.Enums.TransactionStatus.Posted,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Type == Domain.Enums.CashMovementType.In)
        {
            activeSession.CashInAmount += request.Amount;
        }
        else
        {
            if (activeSession.OpeningCashAmount + activeSession.CashSalesAmount + activeSession.CashInAmount - activeSession.CashOutAmount < request.Amount)
            {
                // In cashdrawer management, we can allow withdrawal below 0, but usually we give a warning or let it succeed.
                // Let's allow it but we can log or warn. Let's just proceed.
            }
            activeSession.CashOutAmount += request.Amount;
        }

        await _cashSessionRepository.UpdateAsync(activeSession, cancellationToken);
        var created = await _cashMovementRepository.AddAsync(movement, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = createdBy,
            Action = "Create",
            EntityName = "CashMovement",
            EntityId = created.Id,
            Module = "CashMovements",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapToResponse(created);
    }

    private CashMovementResponse MapToResponse(CashMovement movement)
    {
        return new CashMovementResponse
        {
            Id = movement.Id,
            Type = movement.MovementType,
            Amount = movement.Amount,
            Description = movement.Notes ?? string.Empty,
            CreatedAt = movement.CreatedAt
        };
    }
}
