using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting.Server;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class DeliveryCreateRequest
    {
        [Required]
        public int AddressId { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Description { get; set; }

        [Required]
        public List<CreateDeliveryProduct> Products { get; set; }
    }

    public class CreateDeliveryProduct
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
    }
}
