using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using System.Text;

namespace IfpaMaui.Platforms.Android
{
    [BroadcastReceiver(Label = "IFPA Calendar Widget", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/calendarwidgetprovider")]
    public class CalendarWidget : AppWidgetProvider
    {
        public override async void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            try 
            {
                var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(CalendarWidget)).Name);
                
                // Get the service provider from your MauiApp
                var services = IPlatformApplication.Current.Services;
                var pinballApi = services.GetService<IPinballRankingApi>();
                var geocoding = services.GetService<IGeocoding>();
                var logger = services.GetService<ILogger<CalendarWidget>>();

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

                var hasUpcomingTournament = false;
                Tournament nextTournament = null;

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
                            tournamentEventType: eventType,
                            tournamentSearchSortMode: TournamentSearchSortMode.StartDate,
                            tournamentSearchSortOrder: TournamentSearchSortOrder.Ascending,
                            totalReturn: 1
                        );

                        if (searchResult?.Tournaments?.Any() == true)
                        {
                            hasUpcomingTournament = true;
                            nextTournament = searchResult.Tournaments.First();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Error fetching tournament data");
                    }
                }

                foreach (var widgetId in appWidgetIds)
                {
                    var remoteViews = new RemoteViews(context.PackageName, Ifpa.Resource.Layout.calendarwidget);
                    
                    if (hasUpcomingTournament && nextTournament != null)
                    {
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.noTournamentsNotification, ViewStates.Gone);
                        remoteViews.SetTextViewText(Ifpa.Resource.Id.tournamentName, nextTournament.TournamentName);
                        
                        var dateText = nextTournament.EventStartDate.ToString("MMM d");
                        if (nextTournament.EventStartDate != nextTournament.EventEndDate)
                        {
                            dateText += " - " + nextTournament.EventEndDate.ToString("MMM d");
                        }
                        remoteViews.SetTextViewText(Ifpa.Resource.Id.tournamentDate, dateText);

                        var location = new StringBuilder();
                        if (!string.IsNullOrEmpty(nextTournament.City))
                        {
                            location.Append(nextTournament.City);
                            if (!string.IsNullOrEmpty(nextTournament.Stateprov))
                            {
                                location.Append(", ").Append(nextTournament.Stateprov);
                            }
                        }
                        remoteViews.SetTextViewText(Ifpa.Resource.Id.tournamentLocation, location.ToString());
                    }
                    else
                    {
                        remoteViews.SetViewVisibility(Ifpa.Resource.Id.noTournamentsNotification, ViewStates.Visible);
                    }

                    // Create an Intent to launch the app when widget is clicked
                    var intent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
                    if (hasUpcomingTournament && nextTournament != null)
                    {
                        // Add tournament ID to launch specific tournament details
                        intent.PutExtra("tournamentId", nextTournament.TournamentId);
                    }

                    var flags = PendingIntentFlags.UpdateCurrent;
                    if (OperatingSystem.IsAndroidVersionAtLeast(23))
                    {
                        flags |= PendingIntentFlags.Immutable;
                    }
                    var pendingIntent = PendingIntent.GetActivity(context, 0, intent, flags);
                    remoteViews.SetOnClickPendingIntent(Ifpa.Resource.Id.widgetBackground, pendingIntent);

                    appWidgetManager.UpdateAppWidget(widgetId, remoteViews);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating calendar widget: {ex.Message}");
            }
        }
    }
}