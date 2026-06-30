using Store.Domain.Enums;
namespace Store.Contracts.Requests.CashMovements;
public class CreateCashMovementRequest { public CashMovementType Type { get; set; } public decimal Amount { get; set; } public string Description { get; set; } = string.Empty; }
