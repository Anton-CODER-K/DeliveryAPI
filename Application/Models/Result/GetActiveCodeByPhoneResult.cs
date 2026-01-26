namespace DeliveryAPI.Application.Models.Result
{
    public class GetActiveCodeByPhoneResult
    {
        public int phoneVerificationCodesId { get; set; }
        public DateTime LastSentAt { get; set; }
        public DateOnly SentDay { get; set; }
        public int SentCountToday { get; set; }
    }
}
