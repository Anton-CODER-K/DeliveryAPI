namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class DeliveryPaymentResult
    {
        public int UserId { get; set; }
        public int Status { get; set; }
        public decimal TotalPrice { get; set; } 
        public int PaymentMethod { get; set; }
    }
}
