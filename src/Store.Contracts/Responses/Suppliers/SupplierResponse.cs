namespace Store.Contracts.Responses.Suppliers;
public class SupplierResponse { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; public string? Notes { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? Address { get; set; } public bool IsActive { get; set; } public DateTime CreatedAt { get; set; } }
