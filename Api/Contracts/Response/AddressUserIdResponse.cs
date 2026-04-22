namespace DeliveryAPI.Api.Contracts.Response
{
    public class AddressUserIdResponse
    {
        public string title { get; set; }
        public int addressId { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string house { get; set; }
        public string? apartment { get; set; }
        public string? entrance { get; set; }
        public string? floor { get; set; }
        public string? comment { get; set; }
        public bool is_default { get; set; }
    }
}
