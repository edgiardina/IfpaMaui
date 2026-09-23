using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using System.Globalization;
using System.Text.Json;
using Color = Android.Graphics.Color;
using Location = Microsoft.Maui.Devices.Sensors.Location;
using SizeF = Android.Util.SizeF;
using Uri = Android.Net.Uri;

namespace Ifpa.Platforms.Android.Widgets
{
    /// <summary>
    /// Month calendar of upcoming tournaments near the Calendar tab's location.
    /// Days with a tournament are filled, today is marked, and the larger sizes
    /// list the next three tournaments. Matches the iOS calendar widget, in
    /// the IFPA colours from widget_colors.xml.
    /// </summary>
    [BroadcastReceiver(Label = "IFPA Calendar", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/calendarwidgetprovider")]
    public class CalendarWidget : AppWidgetProvider
    {
        /// <summary>How far ahead the widget looks: the rest of this month and the list.</summary>
        private const int DaysAhead = 62;
        private const int RowCount = 3;

        private const string CacheKey = "CalendarWidgetTournaments";
        private const string CacheFilterKey = "CalendarWidgetTournamentsFilter";

        private static readonly Location Chicago = new(41.8781, -87.6298);

        private enum Size { Small, Medium, Large }

        private readonly IPinballRankingApi pinballApi;
        private readonly IGeocoding geocoding;
        private readonly ILogger<CalendarWidget> logger;

        public CalendarWidget()
        {
            var services = IPlatformApplication.Current?.Services;
            pinballApi = services?.GetService<IPinballRankingApi>();
            geocoding = services?.GetService<IGeocoding>();
            logger = services?.GetService<ILogger<CalendarWidget>>();
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            // The search is a network call. GoAsync keeps the receiver alive
            // until it finishes, instead of letting the system stop the
            // process as soon as OnReceive returns.
            var pending = GoAsync();
            _ = UpdateAsync(context, appWidgetManager, appWidgetIds, pending);
        }

        private async Task UpdateAsync(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds, PendingResult pending)
        {
            try
            {
                var tournaments = await LoadTournaments();

                foreach (var widgetId in appWidgetIds)
                {
                    appWidgetManager.UpdateAppWidget(widgetId, BuildViews(context, appWidgetManager, widgetId, tournaments));
                }

                ScheduleMidnightUpdate(context);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error updating calendar widget");
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

        public override void OnDisabled(Context context)
        {
            var alarms = (AlarmManager)context.GetSystemService(Context.AlarmService);
            alarms?.Cancel(UpdateBroadcast(context, AllWidgetIds(context)));
            base.OnDisabled(context);
        }

        public static void RequestUpdate(Context context)
        {
            var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<CalendarWidget>>();
            var ids = AllWidgetIds(context);
            logger?.LogDebug("CalendarWidget RequestUpdate called — {Count} widget(s) found", ids?.Length ?? 0);
            if (ids?.Length > 0)
            {
                RequestUpdate(context, ids);
            }
        }

        private static void RequestUpdate(Context context, int[] ids)
        {
            var updateIntent = new Intent(context, typeof(CalendarWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(updateIntent);
        }

        private static int[] AllWidgetIds(Context context)
        {
            var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name);
            return AppWidgetManager.GetInstance(context).GetAppWidgetIds(me);
        }

        // MARK: Data

        /// <summary>
        /// Searches with the Calendar tab's filter. A failed search falls back
        /// to the last one that succeeded for the same filter. Null means no
        /// search has ever succeeded for it.
        /// </summary>
        private async Task<IReadOnlyList<WidgetTournament>> LoadTournaments()
        {
            var filter = FilterSignature();

            try
            {
                var location = await ResolveLocation();

                var tournamentType = Settings.CalendarRankingSystem == "All"
                    ? (TournamentType?)null
                    : Enum.Parse<TournamentType>(Settings.CalendarRankingSystem);
                TournamentEventType? eventType = Settings.CalendarShowLeagues ? null : TournamentEventType.Tournament;

                var today = DateTime.Today;
                var result = await pinballApi.TournamentSearch(
                    latitude: location.Latitude,
                    longitude: location.Longitude,
                    radius: Settings.LastCalendarDistance,
                    distanceType: DistanceType.Miles,
                    startDate: today,
                    endDate: today.AddDays(DaysAhead),
                    tournamentType: tournamentType,
                    tournamentEventType: eventType,
                    totalReturn: 250);

                var tournaments = (result?.Tournaments ?? Array.Empty<Tournament>())
                    .Select(n => new WidgetTournament(
                        n.TournamentId,
                        n.TournamentName,
                        n.City,
                        n.Stateprov,
                        n.EventStartDate.DateTime.Date,
                        n.EventEndDate is { } end && end.DateTime.Date > n.EventStartDate.DateTime.Date
                            ? end.DateTime.Date
                            : n.EventStartDate.DateTime.Date))
                    .ToList();

                Preferences.Default.Set(CacheKey, JsonSerializer.Serialize(tournaments));
                Preferences.Default.Set(CacheFilterKey, filter);

                return tournaments;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Calendar widget search failed, using the last result for this filter");

                if (Preferences.Default.Get(CacheFilterKey, string.Empty) != filter)
                    return null;

                try
                {
                    return JsonSerializer.Deserialize<List<WidgetTournament>>(Preferences.Default.Get(CacheKey, "[]"));
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        private static string FilterSignature() =>
            $"{Settings.LastCalendarLocation}|{Settings.LastCalendarDistance}|{Settings.CalendarRankingSystem}|{Settings.CalendarShowLeagues}";

        /// <summary>
        /// The Calendar tab's location. If it does not geocode, the device's
        /// last known location when permission allows, else Chicago.
        /// </summary>
        private async Task<Location> ResolveLocation()
        {
            try
            {
                var locations = await geocoding.GetLocationsAsync(Settings.LastCalendarLocation);
                var first = locations?.FirstOrDefault();
                if (first != null)
                    return first;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not geocode {Location}", Settings.LastCalendarLocation);
            }

            if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted)
            {
                var device = await Geolocation.GetLastKnownLocationAsync();
                if (device != null)
                    return new Location(device.Latitude, device.Longitude);
            }

            return Chicago;
        }

        // MARK: Views

        private RemoteViews BuildViews(Context context, AppWidgetManager appWidgetManager, int widgetId, IReadOnlyList<WidgetTournament> tournaments)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                // The launcher picks the largest layout that fits, and swaps
                // layouts on resize without waking the widget.
                return new RemoteViews(new Dictionary<SizeF, RemoteViews>
                {
                    [new SizeF(110, 100)] = Build(context, widgetId, Size.Small, tournaments),
                    [new SizeF(250, 100)] = Build(context, widgetId, Size.Medium, tournaments),
                    [new SizeF(250, 250)] = Build(context, widgetId, Size.Large, tournaments),
                });
            }

            var options = appWidgetManager.GetAppWidgetOptions(widgetId);
            var width = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            var height = options.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight);
            var size = width < 250 ? Size.Small : height < 250 ? Size.Medium : Size.Large;
            return Build(context, widgetId, size, tournaments);
        }

        private RemoteViews Build(Context context, int widgetId, Size size, IReadOnlyList<WidgetTournament> tournaments)
        {
            var layout = size switch
            {
                Size.Small => Resource.Layout.calendar_widget_small,
                Size.Medium => Resource.Layout.calendar_widget_medium,
                _ => Resource.Layout.calendar_widget_large,
            };
            var views = new RemoteViews(context.PackageName, layout);

            var culture = CultureInfo.CurrentCulture;
            var today = DateTime.Today;
            var upcoming = CalendarWidgetModel.Upcoming(tournaments ?? Array.Empty<WidgetTournament>(), today);

            views.SetTextViewText(Resource.Id.monthTitle,
                today.ToString(size == Size.Large ? "MMMM yyyy" : "MMMM", culture).ToUpper(culture));

            if (size == Size.Large)
            {
                views.SetTextViewText(Resource.Id.filterText, $"{Settings.LastCalendarDistance} mi · {Settings.LastCalendarLocation}");
            }

            FillMonth(context, views, today, CalendarWidgetModel.EventDays(upcoming), size == Size.Large, culture);

            if (size != Size.Small)
            {
                FillRows(context, views, widgetId, upcoming, tournaments == null, culture);
            }

            // Everything outside a tournament row opens the Calendar tab.
            views.SetOnClickPendingIntent(global::Android.Resource.Id.Background,
                OpenApp(context, CalendarWidgetModel.CalendarTabUrl, requestCode: widgetId * 10));

            return views;
        }

        private static readonly int[] WeekdayIds =
        {
            Resource.Id.weekday0, Resource.Id.weekday1, Resource.Id.weekday2, Resource.Id.weekday3,
            Resource.Id.weekday4, Resource.Id.weekday5, Resource.Id.weekday6,
        };

        private static readonly int[] WeekIds =
        {
            Resource.Id.week0, Resource.Id.week1, Resource.Id.week2,
            Resource.Id.week3, Resource.Id.week4, Resource.Id.week5,
        };

        private static readonly int[] DayIds =
        {
            Resource.Id.day0, Resource.Id.day1, Resource.Id.day2, Resource.Id.day3, Resource.Id.day4, Resource.Id.day5, Resource.Id.day6,
            Resource.Id.day7, Resource.Id.day8, Resource.Id.day9, Resource.Id.day10, Resource.Id.day11, Resource.Id.day12, Resource.Id.day13,
            Resource.Id.day14, Resource.Id.day15, Resource.Id.day16, Resource.Id.day17, Resource.Id.day18, Resource.Id.day19, Resource.Id.day20,
            Resource.Id.day21, Resource.Id.day22, Resource.Id.day23, Resource.Id.day24, Resource.Id.day25, Resource.Id.day26, Resource.Id.day27,
            Resource.Id.day28, Resource.Id.day29, Resource.Id.day30, Resource.Id.day31, Resource.Id.day32, Resource.Id.day33, Resource.Id.day34,
            Resource.Id.day35, Resource.Id.day36, Resource.Id.day37, Resource.Id.day38, Resource.Id.day39, Resource.Id.day40, Resource.Id.day41,
        };

        private static void FillMonth(Context context, RemoteViews views, DateTime today, HashSet<DateTime> eventDays, bool large, CultureInfo culture)
        {
            var symbols = CalendarWidgetModel.WeekdaySymbols(culture);
            for (var i = 0; i < 7; i++)
            {
                views.SetTextViewText(WeekdayIds[i], symbols[i]);
            }

            var weeks = CalendarWidgetModel.Weeks(today, culture.DateTimeFormat.FirstDayOfWeek);

            // A month needs four to six rows. Unused rows collapse, so the
            // rest share the height.
            for (var row = 0; row < WeekIds.Length; row++)
            {
                views.SetViewVisibility(WeekIds[row], row < weeks.Count ? ViewStates.Visible : ViewStates.Gone);
            }

            for (var index = 0; index < DayIds.Length; index++)
            {
                var id = DayIds[index];
                var day = index / 7 < weeks.Count ? weeks[index / 7][index % 7] : null;

                if (day is not { } date)
                {
                    views.SetTextViewText(id, string.Empty);
                    views.SetInt(id, "setBackgroundResource", 0);
                    continue;
                }

                var isToday = date == today;
                var hasEvent = eventDays.Contains(date);
                var text = date.Day.ToString(culture);

                views.SetTextViewText(id, hasEvent || isToday ? Bold(text) : new Java.Lang.String(text));
                SetTextColor(context, views, id, (hasEvent, isToday) switch
                {
                    (true, true) => Resource.Color.widgetOnEventToday,
                    (false, true) => Resource.Color.widgetOnToday,
                    (true, false) => Resource.Color.widgetOnPrimaryContainer,
                    _ => date < today ? Resource.Color.widgetOutline : Resource.Color.widgetOnSurface,
                });
                views.SetInt(id, "setBackgroundResource", DayBackground(hasEvent, isToday, large));
            }
        }

        private static int DayBackground(bool hasEvent, bool isToday, bool large) => (hasEvent, isToday, large) switch
        {
            (true, true, false) => Resource.Drawable.calendar_day_event_today_small,
            (true, true, true) => Resource.Drawable.calendar_day_event_today_large,
            (true, false, false) => Resource.Drawable.calendar_day_event_small,
            (true, false, true) => Resource.Drawable.calendar_day_event_large,
            (false, true, false) => Resource.Drawable.calendar_day_today_small,
            (false, true, true) => Resource.Drawable.calendar_day_today_large,
            _ => 0,
        };

        private static readonly int[] RowIds = { Resource.Id.row0, Resource.Id.row1, Resource.Id.row2 };
        private static readonly int[] RowMonthIds = { Resource.Id.rowMonth0, Resource.Id.rowMonth1, Resource.Id.rowMonth2 };
        private static readonly int[] RowDayIds = { Resource.Id.rowDay0, Resource.Id.rowDay1, Resource.Id.rowDay2 };
        private static readonly int[] RowNameIds = { Resource.Id.rowName0, Resource.Id.rowName1, Resource.Id.rowName2 };
        private static readonly int[] RowDetailIds = { Resource.Id.rowDetail0, Resource.Id.rowDetail1, Resource.Id.rowDetail2 };

        private static void FillRows(Context context, RemoteViews views, int widgetId, IReadOnlyList<WidgetTournament> upcoming, bool neverLoaded, CultureInfo culture)
        {
            for (var i = 0; i < RowCount; i++)
            {
                if (i >= upcoming.Count)
                {
                    views.SetViewVisibility(RowIds[i], ViewStates.Gone);
                    continue;
                }

                var tournament = upcoming[i];
                views.SetViewVisibility(RowIds[i], ViewStates.Visible);
                views.SetTextViewText(RowMonthIds[i], tournament.StartDay.ToString("MMM", culture).ToUpper(culture));
                views.SetTextViewText(RowDayIds[i], tournament.StartDay.Day.ToString(culture));
                views.SetTextViewText(RowNameIds[i], tournament.Name);
                views.SetTextViewText(RowDetailIds[i], CalendarWidgetModel.Detail(tournament, culture));
                views.SetOnClickPendingIntent(RowIds[i], OpenApp(context, tournament.Url, requestCode: widgetId * 10 + i + 1));
            }

            if (upcoming.Count == 0)
            {
                views.SetViewVisibility(Resource.Id.emptyMessage, ViewStates.Visible);
                views.SetTextViewText(Resource.Id.emptyMessage, neverLoaded
                    ? "Tournaments not available."
                    : $"No tournaments within {Settings.LastCalendarDistance} mi of {Settings.LastCalendarLocation}.");
            }
            else
            {
                views.SetViewVisibility(Resource.Id.emptyMessage, ViewStates.Gone);
            }
        }

        private static void SetTextColor(Context context, RemoteViews views, int viewId, int colorResource)
            => views.SetTextColor(viewId, new Color(context.GetColor(colorResource)));

        private static Java.Lang.ICharSequence Bold(string text)
        {
            var span = new SpannableString(text);
            span.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, text.Length, SpanTypes.ExclusiveExclusive);
            return span;
        }

        // MARK: Intents

        /// <summary>
        /// Opens MainActivity with a link that DeepLinkService routes. The
        /// intent names the activity, so it does not depend on the activity's
        /// intent filters.
        /// </summary>
        private static PendingIntent OpenApp(Context context, string url, int requestCode)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(Intent.ActionView);
            intent.SetData(Uri.Parse(url));
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

            return PendingIntent.GetActivity(context, requestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }

        private static PendingIntent UpdateBroadcast(Context context, int[] ids)
        {
            var intent = new Intent(context, typeof(CalendarWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            return PendingIntent.GetBroadcast(context, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }

        /// <summary>
        /// Refreshes just after midnight, so the today ring moves and ended
        /// tournaments drop off. The alarm is inexact, which is fine for this.
        /// </summary>
        private static void ScheduleMidnightUpdate(Context context)
        {
            var alarms = (AlarmManager)context.GetSystemService(Context.AlarmService);
            if (alarms == null)
                return;

            var midnight = new DateTimeOffset(DateTime.Today.AddDays(1).AddMinutes(1));
            alarms.Set(AlarmType.Rtc, midnight.ToUnixTimeMilliseconds(), UpdateBroadcast(context, AllWidgetIds(context)));
        }
    }
}
