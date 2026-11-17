namespace Bugbox.Core.Exceptions;

public class BadRequestException : Exception
{
    public IDictionary<string, string[]>? Errors { get; }

    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}
