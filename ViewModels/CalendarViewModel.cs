using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using Plugin.Maui.NativeCalendar;
using System.Diagnostics;
using TournamentSearch = PinballApi.Models.WPPR.Universal.Tournaments.Search.Tournament;

namespace Ifpa.ViewModels
{
    public enum CalendarType
    {
        MapAndList,
        Calendar
    }

    public partial class CalendarViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<NativeCalendarEvent> tournamentCalendarItems = new List<NativeCalendarEvent>();

        [ObservableProperty]
        private List<TournamentWithDistance> selectedDateCalendarItems = new List<TournamentWithDistance>();

        [ObservableProperty]
        private List<TournamentSearch> tournaments = new List<TournamentSearch>();

        public DateTime SelectedDate { get; set; } = DateTime.Today;

        [ObservableProperty]
        private CalendarType currentType = CalendarType.MapAndList;

        [ObservableProperty]
        private List<Pin> pins = new List<Pin>();

        [ObservableProperty]
        private string selectedRankingSystem = Settings.CalendarRankingSystem;

        [ObservableProperty]
        private TournamentSearch selectedCalendarItem;

        private Location LastGeolocation { get; set; }

        private readonly IPinballRankingApi pinballRankingApi;
        private readonly IGeocoding geocoding;

        public CalendarViewModel(IPinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarViewModel> logger) : base(logger)
        {
            this.pinballRankingApi = pinballRankingApi;
            this.geocoding = geocoding;
        }

        [RelayCommand]
        public async Task ChangeCalendarDisplay()
        {
            CurrentType = CurrentType == CalendarType.MapAndList ? CalendarType.Calendar : CalendarType.MapAndList;
            await Task.CompletedTask;
        }

        [RelayCommand]
        public async Task ShowCalendarDetail()
        {
            await Shell.Current.GoToAsync($"calendar-detail?tournamentId={SelectedCalendarItem.TournamentId}");
            SelectedCalendarItem = null;
        }

        public async Task LoadItems(Location geoLocation, int distance)
        {
            IsBusy = true;

            try
            {
                var sw = Stopwatch.StartNew();

                logger.LogDebug("Cleared collections in {0}", sw.ElapsedMilliseconds);

                LastGeolocation = geoLocation;

                var longitude = geoLocation?.Longitude;
                var latitude = geoLocation?.Latitude;

                if (longitude == null || latitude == null)
                {
                    logger.LogWarning("Unable to geocode address {0}", geoLocation);
                    return;
                }

                var tournamentType = (TournamentType?)(Settings.CalendarRankingSystem == "All" ? null : Enum.Parse(typeof(TournamentType), Settings.CalendarRankingSystem));
                TournamentEventType? eventType = Settings.CalendarShowLeagues ? null : TournamentEventType.Tournament;

                var items = await pinballRankingApi.TournamentSearch(latitude,
                                                                     longitude,
                                                                     distance, DistanceType.Miles,
                                                                     startDate: DateTime.Now,
                                                                     endDate: DateTime.Now.AddYears(1),
                                                                     tournamentType: tournamentType,
                                                                     tournamentEventType: eventType,
                                                                     totalReturn: 500);

                logger.LogDebug("Api call completed at {0}", sw.ElapsedMilliseconds);

                if (items.Tournaments.Any())
                {
                    Tournaments = items.Tournaments.OrderBy(n => n.EventStartDate).ToList();

                    Pins = Tournaments
                                .Distinct(new TournamentLocationComparer()) // Get distinct tournaments by location
                                .Select(detail => new Pin
                                {
                                    Location = new Location(detail.Latitude, detail.Longitude),
                                    Label = detail.TournamentName,
                                    Address = $"{detail.Address1} {detail.City}, {detail.Stateprov}",
                                    Type = PinType.Generic,
                                    MarkerId = detail.TournamentId.ToString()
                                }).ToList();

                    TournamentCalendarItems = items.Tournaments
                                  .Select(n => new TournamentWithDistance(n, (long)Location.CalculateDistance(latitude.Value, longitude.Value, n.Latitude, n.Longitude, DistanceUnits.Miles)))
                                  .Select(n => new NativeCalendarEvent
                                  {
                                      Location = n.Address1 + " " + n.City + ", " + n.Stateprov,
                                      Title = n.TournamentName,
                                      StartDate = n.EventStartDate.DateTime,
                                      EndDate = n.EventEndDate.DateTime
                                  })
                                  .ToList();

                    SelectedDateChanged(new DateChangedEventArgs(SelectedDate, SelectedDate));
                }

                logger.LogDebug("Collections loaded at {0}", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading calendar items");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void SelectedDateChanged(DateChangedEventArgs e)
        {
            var longitude = LastGeolocation?.Longitude;
            var latitude = LastGeolocation?.Latitude;
            // NativeCalendarView.Events.Any(e => e.StartDate.Date <= new DateTime(year, month + 1, day) && e.EndDate.Date >= new DateTime(year, month + 1, day)))
            SelectedDateCalendarItems = Tournaments.Where(n => n.EventStartDate.Date <= e.NewDate && n.EventEndDate.Date >= e.NewDate)
                                                   .Select(n => new TournamentWithDistance(n, (long)Location.CalculateDistance(latitude.Value, longitude.Value, n.Latitude, n.Longitude, DistanceUnits.Miles)))
                                                   .ToList();
        }
    }
    public class TournamentLocationComparer : IEqualityComparer<TournamentSearch>
    {
        public bool Equals(TournamentSearch x, TournamentSearch y)
        {
            if (x == null || y == null)
                return false;

            return x.Latitude == y.Latitude && x.Longitude == y.Longitude;
        }

        public int GetHashCode(TournamentSearch obj)
        {
            return obj.Latitude.GetHashCode() ^ obj.Longitude.GetHashCode();
        }
    }
}