using System.Net;
using System.Text.Json;
using BLL.Exceptions;
namespace API.Middlewares;


public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse 
        { 
            Success = false,
            Message = exception.Message 
        };

        switch (exception)
        {
            case BadRequestException e:
                // Nếu là BadRequestException, ta set status 400 và lấy list Errors ra
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Errors = e.Errors; // Gán danh sách lỗi chi tiết vào response
                break;
            
            case NotFoundException _:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case GeminiApiException e:
                context.Response.StatusCode = (int)e.StatusCode;
                break;

            default: // Các lỗi khác, ta set status 500
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = exception.Message;
                break;
        }

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}

// Class dùng để trả về JSON
public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string[]? Errors { get; set; }
}
