namespace DeliveryAPI.Application.Models.Input
{
    public class UserRegisterInput
    {
        public string Number_Phone { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public DateTime Birthday { get; set; }
    }
}
