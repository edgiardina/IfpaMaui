using EventKit;
using Foundation;
using Ifpa.Interfaces;
using Ifpa.ViewModels;
using UIKit;

namespace Ifpa.Services
{
    public class ReminderService : IReminderService
    {
        protected EKEventStore eventStore = new EKEventStore();

        public async Task<bool> CreateReminder(CalendarDetailViewModel calendarDetail, string calendarIdentifier)
        {
            Tuple<bool, NSError> granted;

            // if ios 17, use RequestWriteOnlyAccessToEventsAsync, otherwise use RequestAccessAsync
            if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
            {                             
                granted = await eventStore.RequestWriteOnlyAccessToEventsAsync();
            }
            else
            {
                granted = await eventStore.RequestAccessAsync(EKEntityType.Event);
            }

            if (granted.Item1)
            {
                var calendars = eventStore.GetCalendars(EKEntityType.Event);
                var selectedCalendar = calendars.Single(n => n.Title == calendarIdentifier);

                EKEvent newEvent = EKEvent.FromStore(eventStore);
                newEvent.StartDate = DateTimeToNSDate(calendarDetail.Tournament.EventStartDate.DateTime);
                newEvent.EndDate = DateTimeToNSDate(calendarDetail.Tournament.EventEndDate.DateTime);
                newEvent.Title = calendarDetail.Tournament.TournamentName;
                newEvent.Notes = calendarDetail.Tournament.Details;
                newEvent.Calendar = selectedCalendar;
                //TODO: we don't get start/end time for these so fix so its not all day
                newEvent.AllDay = true;
                newEvent.Location = $"{calendarDetail.Tournament.Address1} {calendarDetail.Tournament.City} {calendarDetail.Tournament.Stateprov}";
                NSError e;
                eventStore.SaveEvent(newEvent, EKSpan.ThisEvent, out e);
                return true;
            }
            else
            {               
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetCalendarList()
        {
            Tuple<bool, NSError> granted;

            if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
            {
                granted = await eventStore.RequestFullAccessToEventsAsync();
            }
            else
            {
                granted = await eventStore.RequestAccessAsync(EKEntityType.Event);
            }

            if (granted.Item1)
            {
                var calendars = eventStore.GetCalendars(EKEntityType.Event)
                                          .Where(n => (n.Type == EKCalendarType.CalDav || n.Type == EKCalendarType.Local) && n.AllowsContentModifications);
          
                return calendars.Select(n => n.Title);
            }
            else
            {
                return new List<string>();
            }
        }

        private DateTime NSDateToDateTime(NSDate date)
        {
            double secs = date.SecondsSinceReferenceDate;
            if (secs < -63113904000)
                return DateTime.MinValue;
            if (secs > 252423993599)
                return DateTime.MaxValue;
            return (DateTime)date;
        }

        private NSDate DateTimeToNSDate(DateTime date)
        {
            if (date.Kind == DateTimeKind.Unspecified)
                date = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return (NSDate)date;
        }


    }
}