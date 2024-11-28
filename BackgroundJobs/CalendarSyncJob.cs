using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using Plugin.Maui.CalendarStore;
using Shiny.Jobs;

namespace Ifpa.BackgroundJobs
{
    public class CalendarSyncJob : IJob
    {
        private readonly ICalendarStore CalendarStore;
        private readonly IPinballRankingApi PinballRankingApi;
        private readonly IGeocoding Geocoder;
        private readonly ILogger<CalendarSyncJob> Logger;

        private const string IFPA_CALENDAR_NAME = "IFPA";
        private Color Color = Color.FromHex("#4579FB");

        public CalendarSyncJob(ICalendarStore calendarStore, IPinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarSyncJob> logger)
        {
            this.CalendarStore = calendarStore;
            this.PinballRankingApi = pinballRankingApi;
            this.Logger = logger;
            this.Geocoder = geocoding;
        }

        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            string newCalendarId = null;

            Logger.LogInformation("Running Calendar Sync Job");

            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();

                if (Settings.SyncCalendarWithSystem && status == PermissionStatus.Granted)
                {
                    Logger.LogInformation("Permissions enabled and Setting for Sync is on");

                    // Create Calendar if it doesn't already exist
                    var calendars = await CalendarStore.GetCalendars();
                    var ifpaCalendar = calendars.FirstOrDefault(c => c.Name == IFPA_CALENDAR_NAME);
                    if (ifpaCalendar == null)
                    {
                        newCalendarId = await CalendarStore.CreateCalendar(IFPA_CALENDAR_NAME, Color);
                    }

                    // Get all future events
                    var geoLocation = await Geocoder.GetLocationsAsync(Settings.LastCalendarLocation);

                    var longitude = geoLocation.FirstOrDefault()?.Longitude;
                    var latitude = geoLocation.FirstOrDefault()?.Latitude;

                    var tournamentType = (TournamentType?)(Settings.CalendarRankingSystem == "All" ? null : Enum.Parse(typeof(TournamentType), Settings.CalendarRankingSystem));

                    var items = await PinballRankingApi.TournamentSearch(latitude,
                                                 longitude,
                                                 Settings.LastCalendarDistance,
                                                 DistanceType.Miles,
                                                 startDate: DateTime.Now,
                                                 endDate: DateTime.Now.AddYears(1),
                                                 tournamentType: tournamentType,
                                                 totalReturn: 500);

                    foreach (var item in items.Tournaments)
                    {
                        // check if ID exists in local DB
                        var localCalendarEvent = await Settings.LocalDatabase.GetLocalCalendarTournamentByTournamentId(item.TournamentId);
                        
                        // TODO: n+1 problem, API should return Details field in search but it doesn't now
                        var localTournamentRecord = await PinballRankingApi.GetTournament((int)item.TournamentId);

                        // If so, update the event
                        if (localCalendarEvent != null)
                        {
                            await CalendarStore.UpdateEvent(localCalendarEvent.LocalCalendarEventID,
                                                            item.TournamentName,
                                                            localTournamentRecord.Details,
                                                            $"{item.Address1}, {item.City}, {item.Stateprov}, {item.CountryName}",
                                                            item.EventStartDate,
                                                            item.EventEndDate,
                                                            true);
                        }
                        // if not, add the event
                        else
                        {
                            var eventId = await CalendarStore.CreateEvent(ifpaCalendar?.Id ?? newCalendarId,
                                                            item.TournamentName,
                                                            localTournamentRecord.Details,
                                                            $"{item.Address1}, {item.City}, {item.Stateprov}, {item.CountryName}",
                                                            item.EventStartDate,
                                                            item.EventEndDate,
                                                            true);

                            await Settings.LocalDatabase.AddLocalCalendarTournament(item.TournamentId, eventId);
                        }
                    }

                    Logger.LogInformation("Calendar Sync Job Complete");
                }
                else
                {
                    Logger.LogInformation("Permissions not enabled or Setting for Sync is off");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error running Calendar Sync Job");
            }
        }
    }
}
