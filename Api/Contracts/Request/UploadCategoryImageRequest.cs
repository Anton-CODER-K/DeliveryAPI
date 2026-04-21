namespace DeliveryAPI.Api.Contracts.Request
{
    public class UploadCategoryImageRequest
    {
        public int CategoryId { get; set; }
        public IFormFile Image { get; set; }
    }
}
