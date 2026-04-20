using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthUploadPhotoRequest
    {
        [Required]
        public IFormFile Photo { get; set; }

    }
}