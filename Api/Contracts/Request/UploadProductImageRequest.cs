namespace DeliveryAPI.Api.Contracts.Request
{
    public class UploadProductImageRequest
    {
        public int ProductId { get; set; }
        public IFormFile Image { get; set; }
    }
}
