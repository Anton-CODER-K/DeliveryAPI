using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Contracts.Webhook;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using DeliveryAPI.Common;

namespace DeliveryAPI.Application.Services
{
    public class LiqPayService
    {
        private readonly string _publicKey;
        private readonly string _privateKey;

        public LiqPayService(IConfiguration config)
        {
            _publicKey = config["LiqPay:PublicKey"];
            _privateKey = config["LiqPay:PrivateKey"];
        }

        public LiqPayCheckoutResponse CreateCheckout(int paymentId, decimal amount)
        {
            var payload = new
            {
                public_key = _publicKey,
                version = "3",
                action = "pay",
                amount = amount,
                currency = "UAH",
                description = $"Delivery payment #{paymentId}",
                order_id = paymentId.ToString(),
                server_url = $"{AppConfigURLBase.BaseUrl}/payments/webhook"
            };

            var json = JsonSerializer.Serialize(payload);

            var data = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(json));

            var signature = CreateSignature(data);

            return new LiqPayCheckoutResponse
            {
                Data = data,
                Signature = signature
            };
        }

        private string CreateSignature(string data)
        {
            var signString = _privateKey + data + _privateKey;

            using var sha1 = SHA1.Create();

            var hash = sha1.ComputeHash(
                Encoding.UTF8.GetBytes(signString));

            return Convert.ToBase64String(hash);
        }

        public LiqPayWebhook ParseWebhook(string data)
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(data));

            return JsonSerializer.Deserialize<LiqPayWebhook>(json);
        }

        public bool ValidateSignature(string data, string signature)
        {
            var expectedSignature = CreateSignature(data);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature));
        }
    }
}
