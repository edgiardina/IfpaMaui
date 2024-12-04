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

        /// <summary>
        /// Periodically check for new notifications to send to users
        /// </summary>
        /// <param name="jobInfo"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            await notificationService.NotifyIfUsersRankChanged();
            await notificationService.NotifyIfUserHasNewlySubmittedTourneyResults();
            await notificationService.NotifyIfNewBlogItemPosted();
            await notificationService.NotifyIfNewCalendarEntry();
        }
    }
}
