using Android.Content;
using Android.Provider;
using Ifpa.Interfaces;
using Ifpa.ViewModels;

namespace Ifpa.Services
{
    public class ReminderService : IReminderService
    {
        public async Task<bool> CreateReminder(CalendarDetailViewModel calendarDetail, string calendarIdentifier)
        {
            Intent intent = new Intent(Intent.ActionInsert);
            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Title, calendarDetail.Tournament.TournamentName);
            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Description, calendarDetail.Tournament.Details);            
            intent.PutExtra(CalendarContract.ExtraEventBeginTime, calendarDetail.Tournament.EventStartDate.ToUnixTimeMilliseconds());
            intent.PutExtra(CalendarContract.ExtraEventEndTime, calendarDetail.Tournament.EventEndDate.ToUnixTimeMilliseconds());

            intent.PutExtra(CalendarContract.EventsColumns.EventLocation, $"{calendarDetail.Tournament.Address1} {calendarDetail.Tournament.City} {calendarDetail.Tournament.Stateprov}");
            intent.PutExtra(CalendarContract.Events.InterfaceConsts.EventTimezone, "UTC");
            intent.PutExtra(CalendarContract.Events.InterfaceConsts.EventEndTimezone, "UTC");
            intent.SetData(CalendarContract.Events.ContentUri);

            intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
            return true;
        }

        public async Task<IEnumerable<string>> GetCalendarList()
        {
            throw new NotImplementedException();
        }
    }
}