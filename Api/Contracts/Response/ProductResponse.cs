namespace DeliveryAPI.Api.Contracts.Response
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int WeightGrams { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public int RestaurantId { get; set; }
    }
}
