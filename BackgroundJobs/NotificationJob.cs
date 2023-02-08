using Shiny.Jobs;

namespace Ifpa.BackgroundJobs
{
    public class NotificationJob : IJob
    {
        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            //var loops = jobInfo.GetValue("LoopCount", 25);

            //for (var i = 0; i < loops; i++)
            //{
            //    if (cancelToken.IsCancellationRequested)
            //        break;

            //    await Task.Delay(1000, cancelToken).ConfigureAwait(false);
            //}

        }
    }
}
