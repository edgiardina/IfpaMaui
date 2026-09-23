using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using System.Globalization;
using Paint = Android.Graphics.Paint;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;
using SizeF = Android.Util.SizeF;

namespace Ifpa.Platforms.Android.Widgets
{
    /// <summary>
    /// Rank of the My Stats player. Carries the same information as the iOS
    /// rank widget, with a Material layout in the IFPA colours: the IFPA number
    /// in the header, then name, rank and points, the profile photo from the
    /// medium size up, and six more statistics in the large size.
    /// </summary>
    [BroadcastReceiver(Label = "IFPA Rank", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/rankwidgetprovider")]
    public class RankWidget : AppWidgetProvider
    {
        /// <summary>
        /// Side of the photo bitmap. Every layout in a responsive widget
        /// carries its own copy through Binder, so the bitmap stays small.
        /// </summary>
        private const int PhotoPixels = 200;

        private enum Size { Compact, Small, Medium, Large }

        private readonly IPinballRankingApi pinballRankingApi;
        private readonly ILogger<RankWidget> logger;

        public RankWidget()
        {
            var services = IPlatformApplication.Current?.Services;
            pinballRankingApi = services?.GetService<IPinballRankingApi>();
            logger = services?.GetService<ILogger<RankWidget>>();
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            // The player and the photo are network calls. GoAsync keeps the
            // receiver alive until they finish.
            var pending = GoAsync();
            _ = UpdateAsync(context, appWidgetManager, appWidgetIds, pending);
        }

        private async Task UpdateAsync(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds, PendingResult pending)
        {
            try
            {
                var (player, photo) = await LoadPlayer();

                foreach (var widgetId in appWidgetIds)
                {
                    appWidgetManager.UpdateAppWidget(widgetId, BuildViews(context, appWidgetManager, widgetId, player, photo));
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error updating rank widget");
            }
            finally
            {
                pending?.Finish();
            }
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            // Android 11 and older have no responsive layouts, so a resize
            // has to pick the layout again.
            RequestUpdate(context, new[] { appWidgetId });
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
        }

        public static void RequestUpdate(Context context)
        {
            var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<RankWidget>>();
            var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(RankWidget)).Name);
            var ids = AppWidgetManager.GetInstance(context).GetAppWidgetIds(me);
            logger?.LogDebug("RankWidget RequestUpdate called — {Count} widget(s) found", ids?.Length ?? 0);
            if (ids?.Length > 0)
            {
                RequestUpdate(context, ids);
            }
        }

        private static void RequestUpdate(Context context, int[] ids)
        {
            var updateIntent = new Intent(context, typeof(RankWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(updateIntent);
        }

        // MARK: Data

        /// <summary>
        /// The My Stats player and a rounded copy of the profile photo. The
        /// player is null when none is chosen or the lookup fails, and the
        /// photo is null when the player has none.
        /// </summary>
        private async Task<(Player Player, Bitmap Photo)> LoadPlayer()
        {
            var playerId = Settings.MyStatsPlayerId;
            if (playerId == 0 || pinballRankingApi == null)
                return (null, null);

            try
            {
                var player = await pinballRankingApi.GetPlayer(playerId);
                return (player, await LoadPhoto(player));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error loading player {PlayerId} for the rank widget", playerId);
                return (null, null);
            }
        }

        /// <summary>
        /// Downloads the photo once per player and keeps it on disk, so a
        /// refresh costs no second download.
        /// </summary>
        private async Task<Bitmap> LoadPhoto(Player player)
        {
            var url = player.ProfilePhoto?.ToString();
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var cacheDir = System.IO.Path.Combine(FileSystem.CacheDirectory, "profilecache");
                Directory.CreateDirectory(cacheDir);
                var path = System.IO.Path.Combine(cacheDir, $"profile_{player.PlayerId}.jpg");

                if (!File.Exists(path))
                {
                    using var httpClient = new HttpClient();
                    await File.WriteAllBytesAsync(path, await httpClient.GetByteArrayAsync(url));
                }

                using var source = BitmapFactory.DecodeFile(path);
                return source == null ? null : RoundedSquare(source, PhotoPixels);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not load the profile photo for player {PlayerId}", player.PlayerId);
                return null;
            }
        }

        /// <summary>
        /// Centre square of the photo with rounded corners. RemoteViews cannot
        /// clip an ImageView on every Android version, so the bitmap carries
        /// the shape.
        /// </summary>
        private static Bitmap RoundedSquare(Bitmap source, int side)
        {
            var crop = Math.Min(source.Width, source.Height);
            var sourceRect = new Rect((source.Width - crop) / 2, (source.Height - crop) / 2,
                                      (source.Width + crop) / 2, (source.Height + crop) / 2);

            var output = Bitmap.CreateBitmap(side, side, Bitmap.Config.Argb8888);
            using var canvas = new Canvas(output);
            using var paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);

            var radius = side * 0.18f;
            canvas.DrawRoundRect(new RectF(0, 0, side, side), radius, radius, paint);
            paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn));
            canvas.DrawBitmap(source, sourceRect, new Rect(0, 0, side, side), paint);

            return output;
        }

        // MARK: Views

        private RemoteViews BuildViews(Context context, AppWidgetManager appWidgetManager, int widgetId, Player player, Bitmap photo)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                // The launcher picks the largest layout that fits, and swaps
                // layouts on resize without waking the widget.
                return new RemoteViews(new Dictionary<SizeF, RemoteViews>
                {
                    [new SizeF(180, 40)] = Build(context, Size.Compact, player, photo),
                    [new SizeF(110, 110)] = Build(context, Size.Small, player, photo),
                    [new SizeF(250, 110)] = Build(context, Size.Medium, player, photo),
                    [new SizeF(250, 250)] = Build(context, Size.Large, player, photo),
                });
            }

            var options = appWidgetManager.GetAppWidgetOptions(widgetId);
            var width = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            var height = options.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight);
            var size = height < 110 ? Size.Compact
                : width < 250 ? Size.Small
                : height < 250 ? Size.Medium
                : Size.Large;
            return Build(context, size, player, photo);
        }

        private static RemoteViews Build(Context context, Size size, Player player, Bitmap photo)
        {
            var layout = size switch
            {
                Size.Compact => Resource.Layout.rank_widget_compact,
                Size.Small => Resource.Layout.rank_widget_small,
                Size.Medium => Resource.Layout.rank_widget_medium,
                _ => Resource.Layout.rank_widget_large,
            };
            var views = new RemoteViews(context.PackageName, layout);

            views.SetOnClickPendingIntent(global::Android.Resource.Id.Background, OpenApp(context));

            if (player == null)
            {
                views.SetViewVisibility(Resource.Id.content, ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.noPlayer, ViewStates.Visible);
                if (Settings.MyStatsPlayerId != 0)
                {
                    views.SetTextViewText(Resource.Id.noPlayer, "Player data not available.");
                }
                return views;
            }

            var culture = CultureInfo.CurrentCulture;
            var stats = player.PlayerStats?.Open;

            views.SetViewVisibility(Resource.Id.content, ViewStates.Visible);
            views.SetViewVisibility(Resource.Id.noPlayer, ViewStates.Gone);

            // The number a player reads out at tournament registration.
            views.SetTextViewText(Resource.Id.ifpaNumber, $"#{player.PlayerId}");
            views.SetTextViewText(Resource.Id.playerName, $"{player.FirstName} {player.LastName}".Trim());
            views.SetTextViewText(Resource.Id.rank, WidgetFormat.Ordinal(stats?.CurrentRank));
            views.SetTextViewText(Resource.Id.points, $"{WidgetFormat.Points(stats?.CurrentPoints, culture)} pts");

            if (size != Size.Small)
            {
                if (photo != null)
                {
                    views.SetViewVisibility(Resource.Id.profilePhoto, ViewStates.Visible);
                    views.SetImageViewBitmap(Resource.Id.profilePhoto, photo);
                }
                else
                {
                    views.SetViewVisibility(Resource.Id.profilePhoto, ViewStates.Gone);
                }
            }

            if (size == Size.Large)
            {
                views.SetTextViewText(Resource.Id.statEffPct, WidgetFormat.Percent(stats?.EfficiencyValue, culture));
                views.SetTextViewText(Resource.Id.statEffRank, WidgetFormat.Ordinal(stats?.EfficiencyRank));
                views.SetTextViewText(Resource.Id.statEvents, WidgetFormat.Count(stats?.TotalEventsAllTime, culture));
                views.SetTextViewText(Resource.Id.statBestFinish, WidgetFormat.Ordinal(stats?.BestFinish));
                views.SetTextViewText(Resource.Id.statAvgFinish, WidgetFormat.Ordinal(stats?.AverageFinish));
                views.SetTextViewText(Resource.Id.statHighestRank, WidgetFormat.Ordinal(stats?.HighestRank));
            }

            return views;
        }

        /// <summary>Opens the app where the user left it.</summary>
        private static PendingIntent OpenApp(Context context)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryLauncher);
            intent.AddFlags(ActivityFlags.NewTask);

            return PendingIntent.GetActivity(context, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }
    }
}
