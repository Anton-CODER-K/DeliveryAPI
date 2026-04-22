namespace DeliveryAPI.Application.Models.Result
{
    public class DeliveryRestaurantResult
    {
        public int DeliveryId { get; set; }

        public string StatusDelivery { get; set; }

        public string PaymentMethod { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
