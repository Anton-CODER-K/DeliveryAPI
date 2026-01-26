namespace DeliveryAPI.Infrastructure.Entity.Record
{
    public class UserRegisterRecord
    {
        public string Number_Phone { get; set; }
        public string Name { get; set; }
        public string Password_Hash { get; set; }
        public DateTime Birthday { get; set; }
    }
}
