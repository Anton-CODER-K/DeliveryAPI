namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class UserByLoginResult
    {
        public int UserId { get; set; }
        public string Roles { get; set; }
        public string HashPassword { get; set; }
    }
}
