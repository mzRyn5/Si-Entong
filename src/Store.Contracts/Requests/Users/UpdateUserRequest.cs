namespace Store.Contracts.Requests.Users;
public class UpdateUserRequest 
{ 
    public string Name { get; set; } = string.Empty; 
    public string Role { get; set; } = "admin"; 
    public bool IsActive { get; set; } = true; 
    public Guid? StoreId { get; set; }
}
