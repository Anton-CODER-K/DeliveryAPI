namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class GetActiveVerificationSessionByTokenHashReadModel
    {
        public int UserId { get; set; }
        public int VerificationSessionId { get; set; }
    }
}
