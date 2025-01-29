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
            _logger.LogInformation("Invoking {decoratedClass}.{methodName}", typeof(T), targetMethod.Name);

            // Get the return type of the method
            Type returnType = targetMethod.ReturnType;
            bool isAsync = typeof(Task).IsAssignableFrom(returnType);

            // Dynamically create the correct fallback policy for the return type
            var createFallbackMethod = typeof(CachingPolicyFactory)
                                            .GetMethod(nameof(CachingPolicyFactory.CreateFallbackPolicy))
                                            ?.MakeGenericMethod(returnType);

            if (createFallbackMethod == null)
                throw new InvalidOperationException($"Could not find a suitable fallback policy for {returnType}");

            var fallbackPolicy = createFallbackMethod.Invoke(null, new object[] { _logger });

            if (isAsync)
            {
                // Ensure we cast the policy to the correct async policy type
                Convert.ChangeType(fallbackPolicy, returnType);
                var asyncFallbackPolicy = fallbackPolicy as IAsyncPolicy<dynamic>;

                // Execute the async method with Polly fallback
                return asyncFallbackPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation("Executing async method {decoratedClass}.{methodName}", typeof(T), targetMethod.Name);
                    var result = targetMethod.Invoke(_decorated, args);

                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                        return returnType.IsGenericType ? ((dynamic)task).Result : null;
                    }

                    return result;
                });
            }
            else
            {
                // Execute the sync method with Polly fallback
                return ((ISyncPolicy)fallbackPolicy).Execute(() =>
                {
                    _logger.LogInformation("Executing sync method {decoratedClass}.{methodName}", typeof(T), targetMethod.Name);
                    return targetMethod.Invoke(_decorated, args);
                });
            }
        }

    }


}
