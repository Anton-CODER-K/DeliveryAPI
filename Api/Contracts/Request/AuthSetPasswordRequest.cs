namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthSetPasswordRequest
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword{ get; set; }
        public DateOnly Birthday { get; set; }

    }
}
