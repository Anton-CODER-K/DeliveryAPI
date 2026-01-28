namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthVerifyRequest
    {
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }
}