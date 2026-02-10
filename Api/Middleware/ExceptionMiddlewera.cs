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
            catch (UnauthorizedException ex)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "UNAUTHORIZED",
                    message = ex.Message
                });
            }
            catch (BusinessException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = ex.Code,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "INTERNAL_ERROR",
                    message = "Internal server error",
                    messageTest = ex.Message
                });
            }
        }
    }

}
