namespace DeliveryAPI.Application.Models.Result
{
    public class DeliveryUserResult
    {
        public int DeliveryId { get; set; }

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
}
