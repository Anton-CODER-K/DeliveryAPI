namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class ProductOrderReadModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int WeightGrams { get; set; }
        public int RestaurantId { get; set; }
        public int Quantity { get; set; }
    }

}
