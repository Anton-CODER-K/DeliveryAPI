namespace DeliveryAPI.Application.Exeptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Unauthorized")
            : base(message)
        {
        }
    }
}
