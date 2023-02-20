using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using PinballApi;
using Microsoft.Maui.Controls.Maps;

namespace Ifpa.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        public ObservableCollection<InlineCalendarItem> TournamentCalenderItems { get; set; } 
        public ObservableCollectionRange<CalendarDetails> CalendarDetails { get; set; }

        public ObservableCollection<Pin> Pins { get; set; }

        public Command LoadItemsCommand { get; set; }

        public CalendarViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            Title = "Calendar";
            CalendarDetails = new ObservableCollectionRange<CalendarDetails>();
            Pins = new ObservableCollection<Pin>();
        }

        public async Task ExecuteLoadItemsCommand(string address, int distance)
        {           
            IsBusy = true;

            try
            {
                var sw = Stopwatch.StartNew();
                CalendarDetails.Clear();
                //InlineCalendarItems.Clear();
                Console.WriteLine("Cleared collections in {0}", sw.ElapsedMilliseconds);

                var items = await PinballRankingApi.GetCalendarSearch(address, distance, DistanceUnit.Miles);

                Console.WriteLine("Api call completed at {0}", sw.ElapsedMilliseconds);
                if (items.Calendar.Any())
                {
                    CalendarDetails.AddRange(items.Calendar.OrderBy(n => n.EndDate));

                    foreach (var detail in CalendarDetails)
                    {
                        LoadEventOntoCalendar(detail);
                    }

                    TournamentCalenderItems = new ObservableCollection<InlineCalendarItem>();

                    foreach (var item in items.Calendar.Where(item => item.EndDate - item.StartDate <= 5.Days()))
                    {
                        TournamentCalenderItems.Add(new InlineCalendarItem()
                        {
                            CalendarId = item.CalendarId,
                            StartTime = item.StartDate,
                            EndTime = item.EndDate,
                            Subject = item.TournamentName,
                            Location = item.City,
                            IsAllDay = true
                        });
                    }

                    OnPropertyChanged("TournamentCalenderItems");
                    OnPropertyChanged("CalendarDetails");
                    OnPropertyChanged("Pins");
                }

                Console.WriteLine("Collections loaded at {0}", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadEventOntoCalendar(CalendarDetails detail)
        {
            var pin = new Pin();
           
            pin.Location = new Location(detail.Latitude, detail.Longitude);
            pin.Label = detail.TournamentName;            
            pin.Address = detail.Address1 + " " + detail.City + ", " + detail.State;
            pin.Type = PinType.Generic;

            Pins.Add(pin);
        }
    }
}