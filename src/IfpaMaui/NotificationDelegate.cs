using Shiny.Notifications;

namespace Ifpa
{
    public class NotificationDelegate : INotificationDelegate
    {
        public async Task OnEntry(NotificationResponse response)
        {
            await App.Current.Dispatcher.DispatchAsync(async () =>
            {
                await Shell.Current.GoToAsync(response.Notification.Payload["url"]);
            });
        }
    }
}