namespace DeliveryAPI.Application.Models.Result
{
    public class DeliveryUserByCourierResult
    {
        public int DeliveryId { get; set; }

        public string StatusDelivery { get; set; }

        public string PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public string UserName { get; set; }
        public string UserPhone { get; set; }
        public DateTime UserBirthday { get; set; }

        public decimal TotalPrice { get; set; }

        public int Total_weight_grams { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<DeliveryUserByCourierItem> Items { get; set; }
    }

    public class DeliveryUserByCourierItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalLineAmount { get; set; }
    }
}
