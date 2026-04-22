namespace DeliveryAPI.Api.Contracts.Request
{
    public class RestaurantUpdateImageRequest
    {
        public int RestaurantId { get; set; }
        public IFormFile Image { get; set; }
    }
}
