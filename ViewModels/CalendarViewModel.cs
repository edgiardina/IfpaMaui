using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using PinballApi;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Calendar.Models;

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
        public ObservableCollectionRange<CalendarDetails> CalendarDetails { get; set; }

        public CalendarType CurrentType { get; set; } = CalendarType.MapAndList;

        public ObservableCollection<Pin> Pins { get; set; }

        public Command LoadItemsCommand { get; set; }

        public Command ChangeCalendarDisplayCommand { get; set; }

        public Command ViewCalendarDetailsCommand { get; set; }

        public CalendarViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<CalendarViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            CalendarDetails = new ObservableCollectionRange<CalendarDetails>();
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
                CalendarDetails.Clear();
                Pins.Clear();

                logger.LogDebug("Cleared collections in {0}", sw.ElapsedMilliseconds);

                var items = await PinballRankingApi.GetCalendarSearch(address, distance, DistanceUnit.Miles);

                logger.LogDebug("Api call completed at {0}", sw.ElapsedMilliseconds);

                if (items.Calendar.Any())
                {
                    CalendarDetails.AddRange(items.Calendar.OrderBy(n => n.EndDate));

                    //Limit calendar to 100 future items. otherwise this page chugs
                    foreach (var detail in CalendarDetails)
                    {
                        LoadEventOntoCalendar(detail);
                    }

                    TournamentCalenderItems = new EventCollection();

                    items.Calendar.Where(item => item.EndDate - item.StartDate <= 5.Days())                                  
                                  .GroupBy(item => item.StartDate.Date)
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

        private void LoadEventOntoCalendar(CalendarDetails detail)
        {
            var location = new Location(detail.Latitude, detail.Longitude);

            //check for duplicate pins at this location. Don't add another pin to the same place.
            if (Pins.Any(n => n.Location == location) == false)
            {
                var pin = new Pin();

                pin.Location = location;
                pin.Label = detail.TournamentName;
                pin.Address = detail.Address1 + " " + detail.City + ", " + detail.State;
                pin.Type = PinType.Generic;
                pin.MarkerId = detail.CalendarId.ToString();

                Pins.Add(pin);
            }
        }
    }
}