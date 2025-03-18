using Shiny.Notifications;

namespace Ifpa
{
    public class NotificationDelegate : INotificationDelegate
    {
        public async Task OnEntry(NotificationResponse response)
        {
            App.Current.Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync(response.Notification.Payload["url"]);
            });
        }
    }
}