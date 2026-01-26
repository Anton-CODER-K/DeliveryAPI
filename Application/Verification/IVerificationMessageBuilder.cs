namespace DeliveryAPI.Application.Verification
{
    public interface IVerificationMessageBuilder
    {
        string Build(string code);
    }
}
