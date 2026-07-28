using System.Text.Json;
using System.Threading.Tasks;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Http;

namespace API.Middlewares
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IMaintenanceService maintenanceService)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Allow bypass for admin routes, auth routes, or swagger
            if (path != null && (
                path.StartsWith("/api/admin") || 
                path.StartsWith("/account/login") || 
                path.StartsWith("/swagger")))
            {
                await _next(context);
                return;
            }

            var config = maintenanceService.GetConfig();
            if (config.IsMaintenance)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    message = string.IsNullOrEmpty(config.Message) ? "System is currently under maintenance. Please try again later." : config.Message,
                    endTime = config.EndTime
                };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
                return;
            }

            await _next(context);
        }
    }
}
