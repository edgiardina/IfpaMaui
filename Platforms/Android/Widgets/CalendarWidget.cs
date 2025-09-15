using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using System.Text;
using static Android.Widget.RemoteViewsService;
using Uri = Android.Net.Uri;

namespace Ifpa.Platforms.Android.Widgets
{
    [BroadcastReceiver(Label = "IFPA Calendar Widget", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/calendarwidgetprovider")]
    public class CalendarWidget : AppWidgetProvider
    {
        private const int MaxTournaments = 10;
        private readonly IPinballRankingApi pinballApi;
        private readonly IGeocoding geocoding;
        private readonly ILogger<CalendarWidget> logger;

        public const string ACTION_TOURNAMENT_CLICKED = "IFPA.CALENDAR.TOURNAMENT_CLICKED";

        public CalendarWidget()
        {
            var services = Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services;
            pinballApi = services.GetService<IPinballRankingApi>();
            geocoding = services.GetService<IGeocoding>();
            logger = services.GetService<ILogger<CalendarWidget>>();
        }

        public override async void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            try
            {
                var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name);

                Location geoLocation = null;
                var defaultLocation = new Location(41.8781, -87.6298); // Chicago

                try
                {
                    geoLocation = (await geocoding.GetLocationsAsync(Settings.LastCalendarLocation)).First();
                }
                catch (Exception e)
                {
                    logger?.LogWarning(e, "Error geolocating");

                    var locationPermission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (locationPermission == PermissionStatus.Granted)
                    {
                        var deviceLocation = await Geolocation.GetLastKnownLocationAsync();
                        if (deviceLocation != null)
                        {
                            geoLocation = new Location(deviceLocation.Latitude, deviceLocation.Longitude);
                        }
                    }

                    if (geoLocation == null)
                    {
                        geoLocation = defaultLocation;
                    }
                }

                List<Tournament> upcomingTournaments = new List<Tournament>();

                if (pinballApi != null && geoLocation != null)
                {
                    try
                    {
                        var tournamentType = (TournamentType?)(Settings.CalendarRankingSystem == "All" ? null :
                            Enum.Parse(typeof(TournamentType), Settings.CalendarRankingSystem));

                        TournamentEventType? eventType = Settings.CalendarShowLeagues ? null : TournamentEventType.Tournament;

                        var searchResult = await pinballApi.TournamentSearch(
                            latitude: geoLocation.Latitude,
                            longitude: geoLocation.Longitude,
                            radius: Settings.LastCalendarDistance,
                            distanceType: DistanceType.Miles,
                            startDate: DateTime.Now,
                            endDate: DateTime.Now.AddYears(1),
                            tournamentType: tournamentType,
                            tournamentSearchSortMode: TournamentSearchSortMode.StartDate,
                            tournamentSearchSortOrder: TournamentSearchSortOrder.Ascending,
                            totalReturn: MaxTournaments
                        );

                        if (searchResult?.Tournaments?.Any() == true)
                        {
                            upcomingTournaments = searchResult.Tournaments.Take(MaxTournaments).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Error fetching tournament data for Calendar Widget");
                    }
                }

                foreach (var widgetId in appWidgetIds)
                {
                    var options = appWidgetManager.GetAppWidgetOptions(widgetId);
                    var minHeight = options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight);
                    var tournamentsToShow = Math.Min(MaxTournaments, Math.Max(1, minHeight / 60));

                    var remoteViews = new RemoteViews(context.PackageName, Resource.Layout.calendarwidget);

                    if (upcomingTournaments.Any())
                    {
                        remoteViews.SetViewVisibility(Resource.Id.noTournamentsNotification, ViewStates.Gone);
                        remoteViews.SetViewVisibility(Resource.Id.tournamentGrid, ViewStates.Visible);

                        // Create adapter for versions before Android 12 (S)
                        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                        {
                            // Create adapter intent
                            var adapterIntent = new Intent(context, typeof(TournamentWidgetService));
                            adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);

                            // Store tournaments in preferences for the service to read
                            var preferences = context.GetSharedPreferences("TournamentWidget", FileCreationMode.Private);
                            var editor = preferences.Edit();
                            editor.PutString($"tournaments_{widgetId}",
                                System.Text.Json.JsonSerializer.Serialize(upcomingTournaments.Take(tournamentsToShow)));
                            editor.Apply();

                            remoteViews.SetRemoteAdapter(Resource.Id.tournamentGrid, adapterIntent);

                            remoteViews.SetEmptyView(Resource.Id.tournamentGrid, Resource.Id.noTournamentsNotification);
                        }
                        else
                        {
                            // Android 12+ use RemoteCollectionItems to avoid obsolete API warnings
                            var builder = new RemoteViews.RemoteCollectionItems.Builder();
                            builder.SetHasStableIds(true);

                            long id = 0;
                            foreach (var t in upcomingTournaments.Take(tournamentsToShow))
                            {
                                var itemView = new RemoteViews(context.PackageName, Resource.Layout.tournament_item);

                                itemView.SetTextViewText(Resource.Id.tournamentName, t.TournamentName);

                                var dateText = t.EventStartDate.DateTime.ToString("MMM d");
                                if (t.EventStartDate != t.EventEndDate)
                                {
                                    dateText += " - " + t.EventEndDate?.DateTime.ToString("MMM d");
                                }
                                itemView.SetTextViewText(Resource.Id.tournamentDate, dateText);

                                var location = new StringBuilder();
                                if (!string.IsNullOrEmpty(t.City))
                                {
                                    location.Append(t.City);
                                    if (!string.IsNullOrEmpty(t.Stateprov))
                                    {
                                        location.Append(", ").Append(t.Stateprov);
                                    }
                                }
                                itemView.SetTextViewText(Resource.Id.tournamentLocation, location.ToString());

                                var fillInIntent = new Intent();
                                fillInIntent.SetAction(ACTION_TOURNAMENT_CLICKED);
                                fillInIntent.PutExtra("TournamentId", t.TournamentId);
                                itemView.SetOnClickFillInIntent(Resource.Id.tournamentItemRoot, fillInIntent);

                                builder.AddItem(id++, itemView);
                            }

                            remoteViews.SetRemoteAdapter(Resource.Id.tournamentGrid, builder.Build());
                        }

                        var templateIntent = new Intent(context, typeof(CalendarWidget));
                        templateIntent.SetAction(ACTION_TOURNAMENT_CLICKED);

                        var flags = PendingIntentFlags.UpdateCurrent | GetMutableIfAndroid12Plus();
                        var pendingIntentTemplate = PendingIntent.GetBroadcast(
                            context,
                            0,
                            templateIntent,
                            flags);

                        remoteViews.SetPendingIntentTemplate(Resource.Id.tournamentGrid, pendingIntentTemplate);
                    }
                    else
                    {
                        remoteViews.SetViewVisibility(Resource.Id.noTournamentsNotification, ViewStates.Visible);
                        remoteViews.SetViewVisibility(Resource.Id.tournamentGrid, ViewStates.Gone);
                    }

                    appWidgetManager.UpdateAppWidget(widgetId, remoteViews);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating calendar widget");
            }
        }

        private static PendingIntentFlags GetMutableIfAndroid12Plus()
        {
            // Avoid referencing PendingIntentFlags.Mutable directly to satisfy CA1416
            return OperatingSystem.IsAndroidVersionAtLeast(31)
                ? (PendingIntentFlags)0x02000000 // FLAG_MUTABLE = 0x02000000
                : 0;
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            // Force a data update when the widget size changes
            var updateIntent = new Intent(context, typeof(CalendarWidget));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, new[] { appWidgetId });
            context.SendBroadcast(updateIntent);

            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
        }

        /// <summary>
        /// This method is called when clicks are registered. Just launch the app for now
        /// </summary>
        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            if (intent?.Action == ACTION_TOURNAMENT_CLICKED)
            {
                var tournamentId = intent.GetLongExtra("TournamentId", -1);
                if (tournamentId > 0)
                {
                    var launchIntent = new Intent(Intent.ActionView, Uri.Parse($"ifpa://tournaments/view.php?t={tournamentId}"));
                    launchIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(launchIntent);
                }
            }
        }
    }

    [Service(Permission = "android.permission.BIND_REMOTEVIEWS", Exported = true)]
    public class TournamentWidgetService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new TournamentRemoteViewsFactory(ApplicationContext, intent);
        }
    }

    public class TournamentRemoteViewsFactory : Java.Lang.Object, IRemoteViewsFactory
    {
        private readonly Context context;
        private readonly int appWidgetId;
        private List<Tournament> tournaments = new List<Tournament>();

        public TournamentRemoteViewsFactory(Context context, Intent intent)
        {
            this.context = context;
            appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
        }

        public void OnCreate()
        {
            // Initialize any resources if needed
        }

        public void OnDestroy()
        {
            // Clean up any resources
            tournaments.Clear();
        }

        public int Count => tournaments.Count;

        public long GetItemId(int position) => position;

        public RemoteViews GetLoadingView() => null; // Use default loading view

        public int ViewTypeCount => 1; // We only have one type of view

        public bool HasStableIds => true;

        public RemoteViews LoadingView => null; // Use default loading view

        public RemoteViews GetViewAt(int position)
        {
            if (position < 0 || position >= tournaments.Count)
                return null;

            var tournament = tournaments[position];
            var itemView = new RemoteViews(context.PackageName, Resource.Layout.tournament_item);

            itemView.SetTextViewText(Resource.Id.tournamentName, tournament.TournamentName);

            var dateText = tournament.EventStartDate.DateTime.ToString("MMM d");
            if (tournament.EventStartDate != tournament.EventEndDate)
            {
                dateText += " - " + tournament.EventEndDate?.DateTime.ToString("MMM d");
            }
            itemView.SetTextViewText(Resource.Id.tournamentDate, dateText);

            var location = new StringBuilder();
            if (!string.IsNullOrEmpty(tournament.City))
            {
                location.Append(tournament.City);
                if (!string.IsNullOrEmpty(tournament.Stateprov))
                {
                    location.Append(", ").Append(tournament.Stateprov);
                }
            }
            itemView.SetTextViewText(Resource.Id.tournamentLocation, location.ToString());

            // Set up the item click
            var fillInIntent = new Intent();
            fillInIntent.SetAction(CalendarWidget.ACTION_TOURNAMENT_CLICKED);
            fillInIntent.PutExtra("TournamentId", tournament.TournamentId);
            itemView.SetOnClickFillInIntent(Resource.Id.tournamentItemRoot, fillInIntent);

            System.Diagnostics.Debug.WriteLine($"Setting fill-in intent for tournament {tournament.TournamentId}");

            return itemView;
        }

        public void OnDataSetChanged()
        {
            var preferences = context.GetSharedPreferences("TournamentWidget", FileCreationMode.Private);
            var tournamentsJson = preferences.GetString($"tournaments_{appWidgetId}", "[]");
            tournaments = System.Text.Json.JsonSerializer.Deserialize<List<Tournament>>(tournamentsJson) ?? new List<Tournament>();
        }
    }
}