namespace DeliveryAPI.Api.Contracts.Request
{
    public class LiqPayWebhookRequest
    {
        public string Data { get; set; }
        public string Signature { get; set; }
    }
}
