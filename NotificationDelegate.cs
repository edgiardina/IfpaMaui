using Shiny.Notifications;

namespace Ifpa
{
    internal class NotificationDelegate : INotificationDelegate
    {
        public async Task OnEntry(NotificationResponse response)
        {
            await Shell.Current.GoToAsync(response.Notification.Payload["url"]);
        }
    }
}
