namespace Store.Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    private readonly string? _errorCode;
    public override string ErrorCode => _errorCode ?? "BUSINESS_RULE_ERROR";

    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, string errorCode) : base(message)
    {
        _errorCode = errorCode;
    }
}
