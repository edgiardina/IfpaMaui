using Ifpa.Services;
using Shiny.Notifications;

namespace Ifpa.Notifications
{
    public class NotificationDelegate : INotificationDelegate
    {
        public async Task OnEntry(NotificationResponse response)
        {
            if (response.Notification.Payload.ContainsKey("url"))
            {
                var url = response.Notification.Payload["url"];

                if (response.ActionIdentifier == NotificationService.NEW_CALENDAR_ADD_ACTION)
                {
                    // Bring user to CalendarDetailViewModel and indicate that they want to add the tournament to their calendar
                    url = url + "&add=true";

                    // bring app to foreground
                }

                Application.Current.Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync(url);
                });
            }
        }
    }
}