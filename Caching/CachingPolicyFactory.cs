using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.Caching;
using Polly;

namespace Ifpa.Caching
{
    public static class PollyPolicyFactory
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
                    return await Task.FromResult(default(object));
                },
                onFallbackAsync: async (context, cancellationToken) =>
                {
                    logger.LogError($"Fallback executed for {context.OperationKey}: No data available.");
                }
            );

            // Cache policy with SQLite
            var cachePolicy = Policy.CacheAsync(
                cacheProvider: cacheProvider,
                ttl: TimeSpan.FromDays(30),
                onCacheGet: (context, key) =>
                {
                    logger.LogWarning($"Cache hit for {context.OperationKey}");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        App.Current.MainPage.DisplayAlert("Cached Data", "Using cached data as the network is unavailable.", "OK");
                    });
                },
                onCacheMiss: (context, key) =>
                {
                    logger.LogInformation($"Cache miss for {context.OperationKey}");
                }
            );

            // Combine fallback and cache policies
            return fallbackPolicy.WrapAsync(cachePolicy);
        }
    }

}
