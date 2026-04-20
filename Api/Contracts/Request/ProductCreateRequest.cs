using System.ComponentModel.DataAnnotations;

namespace DeliveryAPI.Api.Contracts.Request
{
    public class ProductCreateRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int WeightGrams { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required]
        public IFormFile Image { get; set; }
    }
}
