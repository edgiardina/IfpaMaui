using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using PinballApi;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using Plugin.Maui.NativeCalendar;
using PinballApi.Models.WPPR.v2.Calendar;
using TournamentSearch = PinballApi.Models.WPPR.Universal.Tournaments.Search.Tournament;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Devices.Sensors;

namespace Ifpa.ViewModels
{
    public enum CalendarType
    {
        MapAndList,
        Calendar
    }

    public class CalendarViewModel : BaseViewModel
    {
        public ObservableCollection<NativeCalendarEvent> TournamentCalendarItems { get; set; } = new ObservableCollection<NativeCalendarEvent>();

        public ObservableCollection<TournamentWithDistance> SelectedDateCalendarItems { get; set; } = new ObservableCollection<TournamentWithDistance>();

        public ObservableCollection<TournamentSearch> Tournaments { get; set; }

        public CalendarType CurrentType { get; set; } = CalendarType.MapAndList;

        public ObservableCollection<Pin> Pins { get; set; }

        public string SelectedRankingSystem => Settings.CalendarRankingSystem;

        public Command LoadItemsCommand { get; set; }

        public Command ChangeCalendarDisplayCommand { get; set; }

        public Command ViewCalendarDetailsCommand { get; set; }

        public Command SelectedDateChangedCommand { get; set; }

        private Location LastGeolocation { get; set; }


        private readonly PinballRankingApi pinballRankingApi;
        private readonly IGeocoding geocoding;

        public CalendarViewModel(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            this.pinballRankingApi = pinballRankingApi;
            this.geocoding = geocoding;

            Tournaments = new ObservableCollection<TournamentSearch>();
            Pins = new ObservableCollection<Pin>();
            ChangeCalendarDisplayCommand = new Command(() => { CurrentType = CurrentType == CalendarType.MapAndList ? CalendarType.Calendar : CalendarType.MapAndList; OnPropertyChanged("CurrentType"); });
            ViewCalendarDetailsCommand = new Command<long>(async (tournamentId) => await ViewCalendarDetails(tournamentId));
            SelectedDateChangedCommand = new Command<DateChangedEventArgs>(SelectedDateChanged);
        }

        private async Task ViewCalendarDetails(long tournamentId)
        {
            await Shell.Current.GoToAsync($"calendar-detail?tournamentId={tournamentId}");
        }

        public async Task ExecuteLoadItemsCommand(Location geoLocation, int distance)
        {
            IsBusy = true;

            try
            {
                var sw = Stopwatch.StartNew();
                Tournaments.Clear();
                Pins.Clear();

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
                    Tournaments = items.Tournaments.OrderBy(n => n.EventStartDate).ToObservableCollection();

                    //Limit calendar to 100 future items. otherwise this page chugs
                    foreach (var detail in Tournaments)
                    {
                        LoadEventOntoCalendar(detail);
                    }

                    TournamentCalendarItems = items.Tournaments
                                  .Select(n => new TournamentWithDistance(n, (long)Location.CalculateDistance(latitude.Value, longitude.Value, n.Latitude, n.Longitude, DistanceUnits.Miles)))
                                  .Select(n => new NativeCalendarEvent
                                  {
                                      Location = n.Address1 + " " + n.City + ", " + n.Stateprov,
                                      Title = n.TournamentName,
                                      StartDate = n.EventStartDate.DateTime,
                                      EndDate = n.EventEndDate.DateTime
                                  })
                                  .ToObservableCollection();

                    OnPropertyChanged(nameof(Tournaments));
                    OnPropertyChanged(nameof(TournamentCalendarItems));
                    OnPropertyChanged(nameof(Pins));
                    OnPropertyChanged(nameof(SelectedRankingSystem));
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

        private void SelectedDateChanged(DateChangedEventArgs e)
        {
            var longitude = LastGeolocation?.Longitude;
            var latitude = LastGeolocation?.Latitude;

            SelectedDateCalendarItems.Clear();

            foreach (var item in Tournaments)
            {
                if (item.EventStartDate.Date == e.NewDate)
                {
                    SelectedDateCalendarItems.Add(new TournamentWithDistance(item, (long)Location.CalculateDistance(latitude.Value, longitude.Value, item.Latitude, item.Longitude, DistanceUnits.Miles)));
                }
            }

            OnPropertyChanged(nameof(SelectedDateCalendarItems));
        }

        private void LoadEventOntoCalendar(TournamentSearch detail)
        {
            var location = new Location(detail.Latitude, detail.Longitude);

            //check for duplicate pins at this location. Don't add another pin to the same place.
            if (Pins.Any(n => n.Location == location) == false)
            {
                var pin = new Pin();

                pin.Location = location;
                pin.Label = detail.TournamentName;
                pin.Address = detail.Address1 + " " + detail.City + ", " + detail.Stateprov;
                pin.Type = PinType.Generic;
                pin.MarkerId = detail.TournamentId.ToString();

                Pins.Add(pin);
            }
        }
    }
}