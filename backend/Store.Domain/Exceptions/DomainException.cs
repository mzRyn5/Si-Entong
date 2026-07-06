namespace Store.Domain.Exceptions;

public class DomainException : Exception
{
    public virtual string ErrorCode => "DOMAIN_ERROR";

    public DomainException(string message) : base(message) { }
}
