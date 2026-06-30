namespace Store.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public override string ErrorCode => "NOT_FOUND";

    public NotFoundException(string message) : base(message) { }
    
    public NotFoundException(string name, object key) : base($"Entity '{name}' ({key}) was not found.") { }
}
