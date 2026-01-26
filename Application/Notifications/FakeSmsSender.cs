namespace DeliveryAPI.Application.FakeSmsSender
{
    public class FakeSmsSender : INotificationSender
    {
        public Task SendAsync(string phone, string message)
        {
            Console.WriteLine($"[SMS] {phone} -> {message}");
            return Task.CompletedTask;
        }
    }
}
