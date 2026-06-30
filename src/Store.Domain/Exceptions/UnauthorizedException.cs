namespace Store.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public override string ErrorCode => "UNAUTHORIZED";

    public UnauthorizedException(string message) : base(message) { }
}
