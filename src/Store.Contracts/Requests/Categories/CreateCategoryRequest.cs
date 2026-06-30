namespace Store.Contracts.Requests.Categories;
public class CreateCategoryRequest { public string Name { get; set; } = string.Empty; public string? Description { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? Address { get; set; } public bool IsActive { get; set; } = true; }
