namespace DeliveryAPI.Api.Middleware
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Unauthorized")
            : base(message)
        {
        }
    }
}
