namespace DeliveryAPI.Application.Verification
{
    public class NumericVerificationCodeGenerator : IVerificationCodeGenerator
    {
        private readonly Random _random = new();

        public string Generate()
        {
            return _random.Next(100000, 999999).ToString();
        }
    }

}
