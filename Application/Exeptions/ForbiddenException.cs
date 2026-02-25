namespace DeliveryAPI.Application.Exeptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Forbidden")
            : base(message)
        {
        }
    }

}
