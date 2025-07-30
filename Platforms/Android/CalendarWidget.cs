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

namespace IfpaMaui.Platforms.Android
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
        private static string TournamentClick = "TournamentClickTag";

        public CalendarWidget()
        {
            var services = Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services;
            this.pinballApi = services.GetService<IPinballRankingApi>();
            this.geocoding = services.GetService<IGeocoding>();
            this.logger = services.GetService<ILogger<CalendarWidget>>();
        }

        private void RegisterClicks(Context context, RemoteViews remoteViews, int[] appWidgetIds, long tournamentId)
        {
            // Create the deep link intent
            var intent = new Intent(Intent.ActionView, Uri.Parse($"ifpa://tournaments/view.php?t={tournamentId}"));
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var pendingIntent = PendingIntent.GetActivity(
                context, 0, intent,
                PendingIntentFlags.UpdateCurrent | (OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : 0));

            remoteViews.SetOnClickPendingIntent(Ifpa.Resource.Id.widgetBackground, pendingIntent);
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
                            endDate: DateTime.Now.AddDays(30),
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

                    var remoteViews = new RemoteViews(context.PackageName, Ifpa.Resource.Layout.calendarwidget);

                    if (upcomingTournaments.Any())
                    {
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.noTournamentsNotification, ViewStates.Gone);
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.tournamentGrid, ViewStates.Visible);

                        var firstTournament = upcomingTournaments.First();
                        RegisterClicks(context, remoteViews, appWidgetIds, firstTournament.TournamentId);

                        // Create adapter intent
                        var adapterIntent = new Intent(context, typeof(TournamentWidgetService));
                        adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);

                        // Store tournaments in preferences
                        var preferences = context.GetSharedPreferences("TournamentWidget", FileCreationMode.Private);
                        var editor = preferences.Edit();
                        editor.PutString($"tournaments_{widgetId}",
                            System.Text.Json.JsonSerializer.Serialize(upcomingTournaments.Take(tournamentsToShow)));
                        editor.Apply();

                        remoteViews.SetRemoteAdapter(Ifpa.Resource.Id.tournamentGrid, adapterIntent);

                        // Set up pending intent template for collection items
                        var templateIntent = new Intent(Intent.ActionView);
                        templateIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                        var templatePendingIntent = PendingIntent.GetActivity(
                            context, 0, templateIntent,
                            PendingIntentFlags.UpdateCurrent | (OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : 0));
                        remoteViews.SetPendingIntentTemplate(Ifpa.Resource.Id.tournamentGrid, templatePendingIntent);
                    }
                    else
                    {
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.noTournamentsNotification, ViewStates.Visible);
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.tournamentGrid, ViewStates.Gone);
                    }

                    appWidgetManager.UpdateAppWidget(widgetId, remoteViews);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating calendar widget: {ex.Message}");
            }
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
            var itemView = new RemoteViews(context.PackageName, Ifpa.Resource.Layout.tournament_item);

            itemView.SetTextViewText(Ifpa.Resource.Id.tournamentName, tournament.TournamentName);

            var dateText = tournament.EventStartDate.ToString("MMM d");
            if (tournament.EventStartDate != tournament.EventEndDate)
            {
                dateText += " - " + tournament.EventEndDate.ToString("MMM d");
            }
            itemView.SetTextViewText(Ifpa.Resource.Id.tournamentDate, dateText);

            var location = new StringBuilder();
            if (!string.IsNullOrEmpty(tournament.City))
            {
                location.Append(tournament.City);
                if (!string.IsNullOrEmpty(tournament.Stateprov))
                {
                    location.Append(", ").Append(tournament.Stateprov);
                }
            }
            itemView.SetTextViewText(Ifpa.Resource.Id.tournamentLocation, location.ToString());

            // Set up the item click
            var fillInIntent = new Intent();
            fillInIntent.SetData(Uri.Parse($"ifpa://tournaments/view.php?t={tournament.TournamentId}"));
            itemView.SetOnClickFillInIntent(Ifpa.Resource.Id.widgetBackground, fillInIntent);

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