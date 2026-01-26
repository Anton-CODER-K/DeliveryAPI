namespace DeliveryAPI.Application.Verification
{
    public class SmsVerificationMessageBuilder : IVerificationMessageBuilder
    {
        public string Build(string code)
        {
            return $"Your verification code is: {code}. Do not share it.";
        }
    }
}
