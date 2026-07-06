using System.Collections.Generic;

namespace Store.Domain.Exceptions;

public class ValidationException : DomainException
{
    public override string ErrorCode => "VALIDATION_ERROR";
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : base("Satu atau lebih error validasi terjadi.")
    {
        Errors = errors;
    }
}
