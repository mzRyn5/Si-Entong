namespace Store.Contracts.Responses;
public class ErrorResponse { public bool Success { get; set; } = false; public string Message { get; set; } = string.Empty; public object? Errors { get; set; } }
