using System.Net;

namespace BLL.Exceptions;

public sealed class GeminiApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
