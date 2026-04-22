namespace DeliveryAPI.Application.Models.Result
{
    public class RestaurantIdAndStatusResult
    {
        public int RestaurantId { get; set; }
        public int Status { get; set; }
    }

    public class RestaurantIdAndStatusPaymentResult
    {
        public int RestaurantId { get; set; }
        public int Status { get; set; }
        public int StatusPayment { get; set; }
        public int PaymentMethod { get; set; }
    }
}
