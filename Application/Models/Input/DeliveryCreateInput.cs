using DeliveryAPI.Api.Contracts.Request;
using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Application.Models.Input
{
    public class DeliveryCreateInput
    {
        public int UserId { get; set; }

        public int AddressId { get; set; }

        public int PaymentMethodId { get; set; }

        public List<CreateDeliveryProduct> Products { get; set; }
    }
}
