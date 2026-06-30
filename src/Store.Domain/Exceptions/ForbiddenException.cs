namespace Store.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public override string ErrorCode => "FORBIDDEN";

    public ForbiddenException(string message) : base(message) { }
}
