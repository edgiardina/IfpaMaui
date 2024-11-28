using CommunityToolkit.Maui.Core.Extensions;
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
using System.Collections.ObjectModel;
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

        public CalendarType CurrentType { get; set; } = CalendarType.MapAndList;

        [ObservableProperty]
        private List<Pin> pins = new List<Pin>();

        public string SelectedRankingSystem => Settings.CalendarRankingSystem;

        public Command ChangeCalendarDisplayCommand { get; set; }

        public Command ViewCalendarDetailsCommand { get; set; }

        private Location LastGeolocation { get; set; }

        private readonly IPinballRankingApi pinballRankingApi;
        private readonly IGeocoding geocoding;

        public CalendarViewModel(PinballRankingApiV2 pinballRankingApiV2, IPinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            this.pinballRankingApi = pinballRankingApi;
            this.geocoding = geocoding;

            ChangeCalendarDisplayCommand = new Command(() => { CurrentType = CurrentType == CalendarType.MapAndList ? CalendarType.Calendar : CalendarType.MapAndList; OnPropertyChanged("CurrentType"); });
            ViewCalendarDetailsCommand = new Command<long>(async (tournamentId) => await ViewCalendarDetails(tournamentId));
        }

        private async Task ViewCalendarDetails(long tournamentId)
        {
            await Shell.Current.GoToAsync($"calendar-detail?tournamentId={tournamentId}");
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

                    OnPropertyChanged(nameof(SelectedRankingSystem));

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