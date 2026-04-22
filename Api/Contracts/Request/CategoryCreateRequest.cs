namespace DeliveryAPI.Api.Contracts.Request
{
    public class CategoryCreateRequest
    {
        public string Name { get; set; }
        public IFormFile Image { get; set; }
    }
}
