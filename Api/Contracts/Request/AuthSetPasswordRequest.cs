using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AuthSetPasswordRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword{ get; set; }

        [Required]
        public DateOnly Birthday { get; set; }

    }
}
