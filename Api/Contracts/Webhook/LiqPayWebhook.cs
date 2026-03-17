namespace DeliveryAPI.Api.Contracts.Webhook
{
    public class LiqPayWebhook
    {
        public string order_id { get; set; }
        public string status { get; set; }
        public string payment_id { get; set; }
        public decimal amount { get; set; }
    }
}
