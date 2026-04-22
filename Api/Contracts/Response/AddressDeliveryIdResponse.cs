namespace DeliveryAPI.Api.Contracts.Response
{
    public class AddressDeliveryIdResponse
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string house { get; set; }
        public string? apartment { get; set; }
        public string? entrance { get; set; }
        public string? floor { get; set; }
        public string? comment { get; set; }
    }
}
