using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthRefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
