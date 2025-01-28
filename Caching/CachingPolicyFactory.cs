using Microsoft.Extensions.Logging;
using Polly.Caching;
using Polly;
using Ifpa.Models;

namespace Ifpa.Caching
{
    public static class CachingPolicyFactory
    {
        public static AsyncPolicy CreatePolicy(IAsyncCacheProvider cacheProvider, ILogger logger)
        {
            // Fallback policy for handling cache misses and no network
            var fallbackPolicy = Policy.Handle<Exception>().FallbackAsync(
                fallbackAction: async (context, cancellationToken) =>
                {
                    logger.LogWarning($"Fallback triggered for {context.OperationKey}: No cached data and no network.");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        App.Current.MainPage.DisplayAlert("No Data", "No cached data is available, and the network is offline.", "OK");
                    });
                    //return await Task.FromResult(default(object));
                },
                onFallbackAsync: async (exception, task) =>
                {
                    logger.LogError($"Fallback executed for {task.OperationKey}: No data available.");
                }
            );

            // Cache policy with SQLite
            var cachePolicy = Policy.CacheAsync(
                cacheProvider: cacheProvider,
                ttl: Settings.CacheDuration,
                onCacheGet: (context, key) =>
                {
                    logger.LogWarning($"Cache hit for {context.OperationKey}");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        App.Current.MainPage.DisplayAlert("Cached Data", "Using cached data as the network is unavailable.", "OK");
                    });
                },                
                onCachePut: (context, key) =>
                {
                    logger.LogInformation($"Data put in cache for {context.OperationKey}");
                },
                onCacheMiss: (context, key) =>
                {
                    logger.LogInformation($"Cache miss for {context.OperationKey}");
                },
                onCacheGetError: (context, key, exception) =>
                {
                    logger.LogError(exception, $"Error getting cached data for {context.OperationKey}");
                },
                onCachePutError: (context, key, exception) =>
                {
                    logger.LogError(exception, $"Error putting data in cache for {context.OperationKey}");
                }
            );

            // Combine fallback and cache policies
            return fallbackPolicy.WrapAsync(cachePolicy);
        }
    }

}
