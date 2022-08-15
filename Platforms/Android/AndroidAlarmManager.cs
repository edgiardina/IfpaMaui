using Android.App;
using Android.Content;
using Ifpa.Droid;
using Android.OS;
using Android.Runtime;

namespace Ifpa.Platforms.Android
{
    public class AndroidAlarmManager
    {
        public static void CreateAlarm()
        {
            var alarmIntent = new Intent(Platform.CurrentActivity, typeof(BackgroundReceiver));
            var pending = PendingIntent.GetBroadcast(Platform.CurrentActivity, 0, alarmIntent, PendingIntentFlags.UpdateCurrent);

            var alarmManager = Platform.CurrentActivity.GetSystemService(Context.AlarmService).JavaCast<AlarmManager>();
            alarmManager.SetInexactRepeating(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 3 * 1000, AlarmManager.IntervalFifteenMinutes, pending);
        }
    }
}
