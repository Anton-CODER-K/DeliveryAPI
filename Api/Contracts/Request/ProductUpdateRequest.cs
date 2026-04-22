namespace DeliveryAPI.Api.Contracts.Request
{
    public class ProductUpdateRequest
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public int? WeightGrams { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        
    }
}
