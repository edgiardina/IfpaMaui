using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using Plugin.Maui.CalendarStore;

namespace Ifpa.Services
{
    public interface ICalendarSyncService
    {
        Task DeleteIfpaDeviceCalendarAndClearLocalEvents();
        Task SyncIfpaCalendarWithDeviceCalendar();
    }

    public class CalendarSyncService : ICalendarSyncService
    {
        private readonly ICalendarStore CalendarStore;
        private readonly IPinballRankingApi PinballRankingApi;
        private readonly IGeocoding Geocoder;
        private readonly ILogger<CalendarSyncService> Logger;

        private const string IFPA_CALENDAR_NAME = "IFPA";
        private Color Color = Color.FromHex("#4579FB");

        public CalendarSyncService(ICalendarStore calendarStore, IPinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarSyncService> logger)
        {
            this.CalendarStore = calendarStore;
            this.PinballRankingApi = pinballRankingApi;
            this.Logger = logger;
            this.Geocoder = geocoding;
        }

        public async Task SyncIfpaCalendarWithDeviceCalendar()
        {
            string newCalendarId = null;

            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();

                if (status == PermissionStatus.Granted)
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

                    Logger.LogInformation("Calendar Sync Complete");
                }
                else
                {
                    Logger.LogInformation("Permissions not enabled or Setting for Sync is off");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error running Calendar Sync");
            }
        }

        public async Task DeleteIfpaDeviceCalendarAndClearLocalEvents()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();

                if (status == PermissionStatus.Granted)
                {
                    var calendars = await CalendarStore.GetCalendars();
                    var ifpaCalendar = calendars.FirstOrDefault(c => c.Name == IFPA_CALENDAR_NAME);

                    if (ifpaCalendar != null)
                    {
                        await CalendarStore.DeleteCalendar(ifpaCalendar.Id);
                    }

                    await Settings.LocalDatabase.ClearLocalCalendarTournaments();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting IFPA Device Calendar and Clearing Local Events");
            }
        }
    }
}
