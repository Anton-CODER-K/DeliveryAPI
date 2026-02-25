using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class AddressCreateRequest
    {
        [MaxLength(30)]
        public string title { get; set; }

        [Required]
        public decimal latitude {  get; set; }

        [Required]
        public decimal longitude { get; set; }

        
        [MaxLength(20)]
        public string house { get; set; }

        [MaxLength(20)]
        public string? apartment { get; set; }

        [MaxLength(10)]
        public string? entrance { get; set; }

        [MaxLength(10)]
        public string? floor { get; set; }

        [MaxLength(100)]
        public string? comment { get; set; }

        [Required]
        public bool is_default { get; set; }

    }
}
