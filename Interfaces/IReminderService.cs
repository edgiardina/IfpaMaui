using Ifpa.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ifpa.Interfaces
{
    public interface IReminderService
    {
        Task<bool> CreateReminder(CalendarDetailViewModel calendarDetail, string calendarIdentifier);

        Task<IEnumerable<string>> GetCalendarList();
    }
}
