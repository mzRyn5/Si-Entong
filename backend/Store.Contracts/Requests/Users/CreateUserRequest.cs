namespace Store.Contracts.Requests.Users;
public class CreateUserRequest 
{ 
    public string Name { get; set; } = string.Empty; 
    public string Username { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty; 
    public string Role { get; set; } = "admin"; 
    public bool IsActive { get; set; } = true; 
    public Guid? StoreId { get; set; }
}
