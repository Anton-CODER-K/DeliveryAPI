using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthVerifyRequest
    {
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Code { get; set; }
    }
}