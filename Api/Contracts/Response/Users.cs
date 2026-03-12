using System.Data;

namespace DeliveryAPI.Api.Contracts.Response
{
    public class Users
    {
        public int userId { get; set; }
        public string? name { get; set; }
        public string phoneNumber { get; set; }
        public DateOnly? birthDay { get; set; }
        public bool isActive {  get; set; }
        public DateTime createdAt { get; set; }
        public bool? isPhoneVerified { get; set; }
        public DateTime? lastSeenAt { get; set; }
        public string role { get; set; }
    }
}
