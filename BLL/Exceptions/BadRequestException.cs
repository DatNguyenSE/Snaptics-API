namespace BLL.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) 
    { 
    }

    public string[]? Errors { get; }

    public BadRequestException(string message, string[] errors) : base(message)
    {
        Errors = errors;
    }
}