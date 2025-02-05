using Android.Content;
using CommunityToolkit.Maui.Alerts;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using System.Reflection;

namespace Ifpa.Caching
{
    public class CachingProxy<T> : DispatchProxy
    {
        private T _decorated;
        //private AsyncPolicy<object> _policy;
        private ILogger _logger;

        public void Configure(T decorated, ILogger logger)
        {
            _decorated = decorated;
            // _policy = policy; 
            _logger = logger;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            try
            {
                _logger.LogInformation("Logging something before invoking {decoratedClass}.{methodName}",
                    _decorated, targetMethod.Name);

                //var policy = Policy<object>
                //            .Handle<Exception>()
                //            .FallbackAsync(
                //    fallbackAction: (context, cancellationToken) =>
                //    {
                //        _logger.LogWarning($"Fallback triggered for {context.OperationKey}: No cached data and no network.");
                //        MainThread.BeginInvokeOnMainThread(() =>
                //        {
                //            App.Current.MainPage.DisplayAlert("No Data", "No cached data is available, and the network is offline.", Strings.OK);
                //        });
                //        return null;
                //    },
                //    onFallbackAsync: async (exception, task) =>
                //    {
                //        _logger.LogError($"Fallback executed for {task.OperationKey}: No data available.");
                //    }
                //);

                //var policy = Policy<object>.Handle<Exception>().Fallback(fallbackAction: (context, cancellationToken) =>
                //{
                //    _logger.LogWarning($"Fallback triggered for {context.OperationKey}: No cached data and no network.");
                //    MainThread.BeginInvokeOnMainThread(() =>
                //    {
                //        App.Current.MainPage.DisplayAlert("No Data", "No cached data is available, and the network is offline.", Strings.OK);
                //    });
                //    return null;
                //},
                //onFallback: (exception, task) =>
                //{
                //    _logger.LogError($"Fallback executed for {task.OperationKey}: No data available.");
                //});

                //var result = policy.ExecuteAndCaptureAsync(async () => targetMethod.Invoke(_decorated, args));

                var result = targetMethod.Invoke(_decorated, args);

                if (result is Task resultTask)
                {
                    resultTask.ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            _logger.LogError(task.Exception,
                                "An unhandled exception was raised during execution of {decoratedClass}.{methodName}",
                                _decorated, targetMethod.Name);

                            if (task.Exception.InnerException is FlurlHttpException flurlException)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    Toast.Make("Unable to load data, a network connection is required.", CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
                                });
                            }
                            else
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    Toast.Make("An error occurred while loading data", CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
                                });
                            }
                        }
                        _logger.LogInformation("Log something after {decoratedClass}.{methodName} completed",
                            _decorated, targetMethod.Name);
                    });
                }
                else
                {
                    _logger.LogInformation("Logging something after method {decoratedClass}.{methodName} completion.",
                        _decorated, targetMethod.Name);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException ?? ex,
                    "Error during invocation of {decoratedClass}.{methodName}",
                    _decorated, targetMethod.Name);
                throw ex.InnerException ?? ex;
            }
        }
    }


}
