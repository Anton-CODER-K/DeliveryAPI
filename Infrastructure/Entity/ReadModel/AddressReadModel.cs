namespace DeliveryAPI.Infrastructure.Entity.ReadModel
{
    public class AddressReadModel
    {
        public int AddressId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string House { get; set; }
        public string? Apartment { get; set; }
        public string? Entrance { get; set; }
        public string? Floor { get; set; }
        public string? Comment { get; set; }
    }

}
