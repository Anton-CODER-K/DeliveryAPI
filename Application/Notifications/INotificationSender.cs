namespace DeliveryAPI.Application.FakeSmsSender
{
    public interface INotificationSender
    {
        Task SendAsync(string phone, string message);
    }
}
