using Ifpa.Services;
using Shiny.Jobs;

namespace Ifpa.BackgroundJobs
{
    public class NotificationJob : IJob
    {
        public NotificationService notificationService { get; set; }

        public NotificationJob(NotificationService notificationService)
        {
            this.notificationService = notificationService;
        }

        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            if (jobInfo.PeriodicTime != null)
            {
                await notificationService.NotifyIfUsersRankChanged();
                await notificationService.NotifyIfUserHasNewlySubmittedTourneyResults();
                await notificationService.NotifyIfNewBlogItemPosted();
                await notificationService.NotifyIfNewCalendarEntry();
            }

        }
    }
}
