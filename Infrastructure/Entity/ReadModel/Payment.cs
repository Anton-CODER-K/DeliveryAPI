namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int DeliveryId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
    }
}
