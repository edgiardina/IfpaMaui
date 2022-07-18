using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui;
using PinballApi.Models.WPPR.v1.Calendar;
using System.Linq;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        public ObservableCollectionRange<CalendarDetails> CalendarDetails { get; set; }
        //public ObservableCollection<CalendarEvent> InlineCalendarItems { get; set; }
        public Command LoadItemsCommand { get; set; }

        public CalendarViewModel(IConfiguration config) : base(config)
        {
            Title = "Calendar";
            CalendarDetails = new ObservableCollectionRange<CalendarDetails>();
            //InlineCalendarItems = new CalendarEventCollectionRange();
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

                    //InlineCalendarItems.AddRange(
                    //    items.Calendar.Where(item => item.EndDate - item.StartDate <= 5.Days())
                    //                  .Select(i => 
                    //                    new InlineCalendarItem
                    //                    {
                    //                        CalendarId = i.CalendarId,
                    //                        Subject = i.TournamentName,
                    //                        StartTime = i.StartDate.Date,
                    //                        EndTime = i.EndDate.Date,
                    //                        IsAllDay = true
                    //                    })
                    //);
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