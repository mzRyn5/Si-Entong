namespace Store.Contracts.Responses.Common;

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public MetaData Meta { get; set; } = new();
}

public class MetaData
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
