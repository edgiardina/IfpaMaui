using Android.Content;
using Android.OS;
using Ifpa.Services;

namespace Ifpa.Droid
{
    [BroadcastReceiver]
    public class BackgroundReceiver : BroadcastReceiver
    {

        public override async void OnReceive(Context context, Intent intent)
        {
            PowerManager pm = (PowerManager)context.GetSystemService(Context.PowerService);
            PowerManager.WakeLock wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "BackgroundReceiver");
            wakeLock.Acquire();

            //TODO: there has to be a better way to resolve this service.
            var notificationService = App.ServiceProvider.GetService<NotificationService>();

            await notificationService.NotifyIfUserHasNewlySubmittedTourneyResults();
            await notificationService.NotifyIfUsersRankChanged();
            await notificationService.NotifyIfNewBlogItemPosted();

            wakeLock.Release();
        }
    }
}