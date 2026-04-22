using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthStartRequest
    {
        [Phone]
        public string PhoneNumber { get; set; } = default!;
    }
}