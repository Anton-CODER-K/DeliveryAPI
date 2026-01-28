namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class ActiveVerificationCodeForVerifyResult
    {
        public int PhoneVerificationCodeId { get; set; }
        public int UserId { get; set; }
        public string CodeHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public int AttemptsLeft { get; set; }
        
    }
}
