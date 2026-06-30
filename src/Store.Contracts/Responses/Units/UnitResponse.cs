namespace Store.Contracts.Responses.Units;
public class UnitResponse { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string? Description { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? Address { get; set; } public bool IsActive { get; set; } public DateTime CreatedAt { get; set; } }
