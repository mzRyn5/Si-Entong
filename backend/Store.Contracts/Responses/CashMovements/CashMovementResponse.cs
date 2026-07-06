using Store.Domain.Enums;
namespace Store.Contracts.Responses.CashMovements;
public class CashMovementResponse { public Guid Id { get; set; } public CashMovementType Type { get; set; } public decimal Amount { get; set; } public string Description { get; set; } = string.Empty; public DateTime CreatedAt { get; set; } }
