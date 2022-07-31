using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        public ObservableCollection<InlineCalendarItem> TournamentCalenderItems { get; set; } 
        public ObservableCollectionRange<CalendarDetails> CalendarDetails { get; set; }

        public Command LoadItemsCommand { get; set; }

        public CalendarViewModel(IConfiguration config) : base(config)
        {
            Title = "Calendar";
            CalendarDetails = new ObservableCollectionRange<CalendarDetails>();      
        }

        public async Task ExecuteLoadItemsCommand(string address, int distance)
        {
            if (IsBusy)
                return;

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
    }
}