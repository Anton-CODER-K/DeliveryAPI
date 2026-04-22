using System.Text.Json;
using DeliveryAPI.Application.Exeptions;

namespace DeliveryAPI.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedException ex)
            {

                _logger.LogWarning(ex, "Unauthorized access");

                _logger.LogError(ex, "Error on {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "UNAUTHORIZED",
                    message = ex.Message
                });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error: {Code}", ex.Code);

                _logger.LogError(ex, "Error on {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = ex.Code,
                    message = ex.Message
                });
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden");

                _logger.LogError(ex, "Error on {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "FORBIDDEN",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                _logger.LogError(ex, "Error on {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "INTERNAL_ERROR",
                    message = "Internal server error"
                });
            }
        }
    }

}
