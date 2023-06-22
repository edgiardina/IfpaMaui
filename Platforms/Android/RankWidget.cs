using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using PinballApi;
using PinballApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Platforms.Android
{
    [BroadcastReceiver(Label = "IFPA Rank Widget", Exported = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    public class RankWidget : AppWidgetProvider
    {
        PinballRankingApiV2 pinballRankingApiV2 { get; set; }

        private static string BackgroundClick = "BackgroundClickTag";

        public RankWidget()
        {
            //TODO: can we dependency inject this PinballRankingApiV2?
            var settings = App.GetAppSettings();
            pinballRankingApiV2 = new PinballRankingApiV2(settings.IfpaApiKey);
        }

        public override async void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(RankWidget)).Name);
            appWidgetManager.UpdateAppWidget(me, await BuildRemoteViews(context, appWidgetIds));
        }

        private async Task<RemoteViews> BuildRemoteViews(Context context, int[] appWidgetIds)
        {
            var widgetView = new RemoteViews(context.PackageName, Resource.Layout.rankwidget);

            await SetTextViewText(widgetView);
            RegisterClicks(context, appWidgetIds, widgetView);

            return widgetView;
        }

        private async Task SetTextViewText(RemoteViews widgetView)
        {
            var playerId = Models.Settings.MyStatsPlayerId;

            if (playerId != 0)
            {
                try
                {
                    widgetView.SetViewVisibility(Resource.Id.selectPlayerNotification, ViewStates.Gone);

                    var player = await pinballRankingApiV2.GetPlayer(playerId);

                    widgetView.SetTextViewText(Resource.Id.widgetName, $"{player.FirstName} {player.LastName}");
                    widgetView.SetTextViewText(Resource.Id.widgetRank, player.PlayerStats.CurrentWpprRank.OrdinalSuffix());
                    widgetView.SetTextViewText(Resource.Id.widgetIfpaNumber, $"# {player.PlayerId}");
                    widgetView.SetTextViewText(Resource.Id.widgetPoints, $"{player.PlayerStats.CurrentWpprValue}");
                }
                catch (Exception ex)
                {
                    Log.Error("ifpa", ex.Message);
                }
            }
            else
            {
                widgetView.SetViewVisibility(Resource.Id.selectPlayerNotification, ViewStates.Visible);
            }
        }

        private void RegisterClicks(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(RankWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);

            // Register click event for the Background
            var piBackground = PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetBackground, piBackground);

            widgetView.SetOnClickPendingIntent(Resource.Id.widgetBackground, GetPendingSelfIntent(context, BackgroundClick));

        }

        private PendingIntent GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(RankWidget));
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, 0);
        }

        /// <summary>
		/// This method is called when clicks are registered. Just launch the app for now
		/// </summary>
		public override void OnReceive(Context context, Intent intent)
        {
            if (BackgroundClick.Equals(intent.Action))
            {
                var pm = context.PackageManager;
                try
                {
                    var packageName = "com.edgiardina.ifpa";
                    var launchIntent = pm.GetLaunchIntentForPackage(packageName);
                    context.StartActivity(launchIntent);
                }
                catch (Exception ex)
                {
                    Log.Error("ifpa", ex.Message);
                }
            }
            base.OnReceive(context, intent);


        }

    }
}
