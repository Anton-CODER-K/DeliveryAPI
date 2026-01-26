using System.Text.Json;

namespace DeliveryAPI.Api.Middleware
{

    public class BusinessException : Exception
    {
        public string Code { get; }

        public BusinessException(string code, string message)
            : base(message)
        {
            Code = code;
        }
    }

    

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = ex.Code,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "UNAUTHORIZED",
                    message = "Unauthorized"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "INTERNAL_ERROR",
                    message = "Internal server error"
                });
            }
        }
    }

}
