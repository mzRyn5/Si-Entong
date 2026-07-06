namespace Store.Contracts.Responses.AuditLogs;
public class AuditLogResponse
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
