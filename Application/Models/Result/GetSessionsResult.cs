namespace DeliveryAPI.Application.Models.Result
{
    public class GetSessionsResult
    {
        public int SessionsId { get; set; }
        public string ip { get; set; }
        public string userAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
