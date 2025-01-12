using Ifpa.Models;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Ifpa.BackgroundJobs
{
    public class CalendarSyncJob : IJob
    {
        private readonly ICalendarSyncService CalendarSyncService;
        private readonly ILogger<CalendarSyncJob> Logger;

        public CalendarSyncJob(ICalendarSyncService calendarSyncService, ILogger<CalendarSyncJob> logger)
        {
            CalendarSyncService = calendarSyncService;
            Logger = logger;
        }

        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            Logger.LogInformation("Running Calendar Sync Job");
            
            if (Settings.SyncCalendarWithSystem)
            {
                Logger.LogInformation("Permission enabled, Syncing calendar with system");
                await CalendarSyncService.SyncIfpaCalendarWithDeviceCalendar();
            }
            else
            {
                Logger.LogInformation("Permission not enabled, skipping calendar sync");
            }
        }
    }
}
