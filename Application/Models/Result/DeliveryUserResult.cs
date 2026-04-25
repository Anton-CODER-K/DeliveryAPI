using DeliveryAPI.Api.Contracts.Response;

namespace DeliveryAPI.Application.Models.Result
{
    public class DeliveryUserResult
    {
        public int DeliveryId { get; set; }

        public string? Description { get; set; }

        public string StatusDelivery { get; set; }

        public string PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public decimal TotalPrice { get; set; }

        public int Total_weight_grams { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<DeliveryUserItem> Items { get; set; }
    }

    public class DeliveryUserItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalLineAmount { get; set; }
    }




    public class DeliveryCourierResult
    {
        public int DeliveryId { get; set; }

        public int RestaurantId { get; set; }

        public int StatusDelivery { get; set; }

        public string PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public decimal TotalPrice { get; set; }

        public int Total_weight_grams { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<DeliveryCourierItem> Items { get; set; }

        public AddressUserIdByCourierResponse Address { get; set; }
       
        public UserResult User {  get; set; }

    }

    public class DeliveryCourierItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalLineAmount { get; set; }
    }

    public class UserResult
    {
        public string Phone { get; set; }
        public string Name { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class AddressUserIdByCourierResponse
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string house { get; set; }
        public string? apartment { get; set; }
        public string? entrance { get; set; }
        public string? floor { get; set; }
        public string? comment { get; set; }
    }

}
