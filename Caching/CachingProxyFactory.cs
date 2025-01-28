using Microsoft.Extensions.Logging;
using Polly;
using System.Reflection;

namespace Ifpa.Caching
{
    public static class CachingProxyFactory
    {
        public static T Create<T>(T decorated, AsyncPolicy policy, ILogger logger) where T : class
        {
            // Create an instance of the proxy
            var proxy = DispatchProxy.Create<T, CachingProxy<T>>() as CachingProxy<T>;

            // Configure the proxy with the decorated instance, policy, and logger
            proxy?.Configure(decorated, policy, logger);

            return proxy as T;
        }
    }
}