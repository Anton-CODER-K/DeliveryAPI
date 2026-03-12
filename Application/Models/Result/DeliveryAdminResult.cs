namespace DeliveryAPI.Application.Models.Result
{
    public class DeliveryAdminResult
    {
        public int DeliveryId { get; set; }

        public int UserId { get; set; }

        public int? CourierId { get; set; }

        public int RestaurantId { get; set; }

        public string StatusDelivery { get; set; }

        public string PaymentMethod { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal ProductPrice { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal CommissionsAmount { get; set; }

        public decimal CommissionPercent { get; set; }

        public int Total_weight_grams { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<DeliveryUserItem> Items { get; set; }
    }

    public class DeliveryAdmintem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalLineAmount { get; set; }
    }
}
