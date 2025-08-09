using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Ifpa.Models;
using PinballApi.Extensions;
using PinballApi.Interfaces;
using AndroidNet = Android.Net;
using Application = Android.App.Application;
using Android.Graphics;

namespace Ifpa.Platforms.Android.Widgets
{
    [BroadcastReceiver(Label = "IFPA Rank Widget", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/rankwidgetprovider")]
    public class RankWidget : AppWidgetProvider
    {
        private readonly IPinballRankingApi PinballRankingApi;
        private readonly AppSettings AppSettings;

        private static string BackgroundClick = "BackgroundClickTag";

        public RankWidget()
        {
            // TODO: Use Dependency Injection to get the PinballRankingApi instance
            PinballRankingApi = Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services.GetService<IPinballRankingApi>();
            AppSettings = Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services.GetService<AppSettings>();
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

                    var player = await PinballRankingApi.GetPlayer(playerId);
                    var playerStats = player.PlayerStats.Open;

                    widgetView.SetTextViewText(Resource.Id.widgetName, $"{player.FirstName} {player.LastName}");
                    widgetView.SetTextViewText(Resource.Id.widgetRank, playerStats.CurrentRank.OrdinalSuffix());
                    widgetView.SetTextViewText(Resource.Id.widgetIfpaNumber, $"# {player.PlayerId}");
                    widgetView.SetTextViewText(Resource.Id.widgetPoints, $"{playerStats.CurrentPoints}");

                    // Set profile photo with local caching and FileProvider
                    var profilePhotoUrl = player.ProfilePhoto != null && !string.IsNullOrWhiteSpace(player.ProfilePhoto.ToString())
                        ? player.ProfilePhoto.ToString()
                        : AppSettings.IfpaPlayerNoProfilePicUrl;

                    string localPath = null;
                    AndroidNet.Uri contentUri = null;
                    if (!string.IsNullOrEmpty(profilePhotoUrl) && profilePhotoUrl.StartsWith("http"))
                    {
                        var fileName = $"profile_{player.PlayerId}.jpg";
                        var cacheDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "profilecache");
                        System.IO.Directory.CreateDirectory(cacheDir);
                        localPath = System.IO.Path.Combine(cacheDir, fileName);

                        if (!File.Exists(localPath))
                        {
                            try
                            {
                                using var httpClient = new HttpClient();
                                var imageBytes = await httpClient.GetByteArrayAsync(profilePhotoUrl);
                                await File.WriteAllBytesAsync(localPath, imageBytes);
                            }
                            catch
                            {
                                localPath = null;
                            }
                        }
                        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                        {
                            var context = Application.Context;
                            contentUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, "com.edgiardina.ifpa.fileprovider", new Java.IO.File(localPath));
                        }
                    }

                    if (contentUri != null)
                    {
                        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                        {
                            var bitmap = BitmapFactory.DecodeFile(localPath);
                            if (bitmap != null)
                                widgetView.SetImageViewBitmap(Resource.Id.profilePhoto, bitmap);
                            else
                                widgetView.SetImageViewResource(Resource.Id.profilePhoto, Resource.Drawable.noplayerpic);
                        }
                        else
                        {
                            widgetView.SetImageViewResource(Resource.Id.profilePhoto, Resource.Drawable.noplayerpic);
                        }
                    }
                    else
                    {
                        widgetView.SetImageViewResource(Resource.Id.profilePhoto, Resource.Drawable.noplayerpic);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("ifpa", ex.Message);
                }
            }
            else
            {
                widgetView.SetViewVisibility(Resource.Id.selectPlayerNotification, ViewStates.Visible);
                widgetView.SetImageViewResource(Resource.Id.profilePhoto, Resource.Drawable.noplayerpic);
            }
        }

        private void RegisterClicks(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(RankWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);

            // Register click event for the Background
            var piBackground = PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.Immutable);

            widgetView.SetOnClickPendingIntent(Resource.Id.widgetBackground, piBackground);

            widgetView.SetOnClickPendingIntent(Resource.Id.widgetBackground, GetPendingSelfIntent(context, BackgroundClick));
        }

        private PendingIntent GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(RankWidget));
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.Immutable);
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
