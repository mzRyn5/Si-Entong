namespace Store.Contracts.Responses.Common;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public ErrorDetails? Error { get; set; }
    public string? TraceId { get; set; }
}

public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public List<ErrorDetail>? Details { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
