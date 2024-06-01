﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using PinballApi;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Calendar.Models;
using PinballApi.Models.WPPR.v2.Calendar;
using PinballApi.Models.WPPR.Universal.Tournaments;

namespace Ifpa.ViewModels
{
    public enum CalendarType
    {
        MapAndList,
        Calendar
    }

    public class CalendarViewModel : BaseViewModel
    {
        public EventCollection TournamentCalenderItems { get; set; } = new EventCollection();
        public ObservableCollectionRange<Tournament> Tournaments { get; set; }

        public CalendarType CurrentType { get; set; } = CalendarType.MapAndList;

        public ObservableCollection<Pin> Pins { get; set; }

        public Command LoadItemsCommand { get; set; }

        public Command ChangeCalendarDisplayCommand { get; set; }

        public Command ViewCalendarDetailsCommand { get; set; }

        private readonly PinballRankingApi pinballRankingApi;
        private readonly IGeocoding geocoding;

        public CalendarViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, IGeocoding geocoding, ILogger<CalendarViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            this.pinballRankingApi = pinballRankingApi;
            this.geocoding = geocoding;

            Tournaments = new ObservableCollectionRange<Tournament>();
            Pins = new ObservableCollection<Pin>();
            ChangeCalendarDisplayCommand = new Command(() => { CurrentType = CurrentType == CalendarType.MapAndList ? CalendarType.Calendar : CalendarType.MapAndList; OnPropertyChanged("CurrentType"); });
            ViewCalendarDetailsCommand = new Command<int>(async (calendarId) => await ViewCalendarDetails(calendarId));
        }

        private async Task ViewCalendarDetails(int calendarId)
        {
            await Shell.Current.GoToAsync($"calendar-detail?calendarId={calendarId}");            
        }

        public async Task ExecuteLoadItemsCommand(string address, int distance)
        {
            IsBusy = true;

            try
            {
                var sw = Stopwatch.StartNew();
                Tournaments.Clear();
                Pins.Clear();

                logger.LogDebug("Cleared collections in {0}", sw.ElapsedMilliseconds);

                //var items = await PinballRankingApi.GetCalendarSearch(address, distance, DistanceUnit.Miles);
                var geoLocation = await geocoding.GetLocationsAsync(address);

                var longitude = geoLocation.First().Longitude;
                var latitude = geoLocation.First().Latitude;

                var items = await pinballRankingApi.TournamentSearch(latitude, longitude, distance, DistanceType.Miles, startDate: DateTime.Now, endDate: DateTime.Now.AddYears(1));

                logger.LogDebug("Api call completed at {0}", sw.ElapsedMilliseconds);

                if (items.Tournaments.Any())
                {
                    Tournaments.AddRange(items.Tournaments.OrderBy(n => n.EventEndDate));

                    //Limit calendar to 100 future items. otherwise this page chugs
                    foreach (var detail in Tournaments)
                    {
                        LoadEventOntoCalendar(detail);
                    }

                    TournamentCalenderItems = new EventCollection();

                    items.Tournaments
                                  .Where(item => item.EventEndDate - item.EventStartDate <= 5.Days())                                  
                                  .Select(n => new TournamentWithDistance(n, (long)Location.CalculateDistance(latitude, longitude, n.Latitude, n.Longitude, DistanceUnits.Miles)))
                                  .GroupBy(item => item.EventStartDate.Date)
                                  .ToList()
                                  .ForEach(date => TournamentCalenderItems.Add(date.Key, date.ToList()));

                    OnPropertyChanged("TournamentCalenderItems");
                    OnPropertyChanged("CalendarDetails");
                    OnPropertyChanged("Pins");
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

        private void LoadEventOntoCalendar(Tournament detail)
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