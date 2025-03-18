using Microsoft.Extensions.Logging;
using Polly.Caching;
using Polly;
using Ifpa.Models;

namespace Ifpa.Caching
{
    public static class CachingPolicyFactory
    {
        public static AsyncPolicy<object> CreatePolicy(IAsyncCacheProvider cacheProvider, ILogger logger)
        {
            // Fallback policy for handling cache misses and no network
            var fallbackPolicy = Policy<object>
                                        .Handle<Exception>()
                                        .FallbackAsync(
                fallbackAction: (context, cancellationToken) =>
                {
                    logger.LogWarning($"Fallback triggered for {context.OperationKey}: No cached data and no network.");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        App.Current.MainPage.DisplayAlert("No Data", "No cached data is available, and the network is offline.", Strings.OK);
                    });
                    return null;
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
                        App.Current.MainPage.DisplayAlert("Cached Data", "Using cached data as the network is unavailable.", Strings.OK);
                    });
                },
                onCachePut: (context, key) =>
                {
                    logger.LogDebug($"Data put in cache for {context.OperationKey}");
                },
                onCacheMiss: (context, key) =>
                {
                    logger.LogDebug($"Cache miss for {context.OperationKey}");
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
            //return fallbackPolicy.WrapAsync(cachePolicy);
            return fallbackPolicy;
        }

        public static IAsyncPolicy<T> CreateFallbackPolicy<T>(ILogger logger)
        {
            return Policy<T>.Handle<Exception>().FallbackAsync(
                fallbackAction: async (context, cancellationToken) =>
                {
                    logger.LogWarning("Fallback triggered: Returning default value for {type}", typeof(T));

                    if (typeof(T) == typeof(Task))
                    {
                        return await Task.FromResult(default(T)); // Return a default Task<T>
                    }
                    else
                    {
                        return default; // Return a default value for sync methods
                    }
                },
                onFallbackAsync: async (exception, task) =>
                {
                    logger.LogError($"Fallback executed for {task.OperationKey}: No data available.");
                }
            );
        }
    }

}
