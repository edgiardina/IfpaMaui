using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Caching
{
    public class CachingProxy<T> : DispatchProxy where T : class
    {
        private T _decorated;
        private AsyncPolicy _policy;
        private ILogger _logger;

        public void Configure(T decorated, AsyncPolicy policy, ILogger logger)
        {
            _decorated = decorated;
            _policy = policy;
            _logger = logger;
        }

        protected override object? Invoke(MethodInfo targetMethod, object[] args)
        {
            // Check if the method is asynchronous
            if (typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
            {
                // Handle async methods
                return InvokeAsync(targetMethod, args);
            }
            else
            {
                // Handle sync methods by wrapping in a Task and using ExecuteAsync
                return _policy.ExecuteAsync(() => Task.Run(() =>
                {
                    _logger.LogInformation($"Executing synchronous method {targetMethod.Name}");
                    return targetMethod.Invoke(_decorated, args);
                })).GetAwaiter().GetResult(); // Unwrap the result for synchronous execution
            }
        }

        private async Task<object?> InvokeAsync(MethodInfo targetMethod, object[] args)
        {
            return await _policy.ExecuteAsync(async () =>
            {
                _logger.LogInformation($"Executing async method {targetMethod.Name}");
                var result = targetMethod.Invoke(_decorated, args);

                if (result is Task task)
                {
                    await task.ConfigureAwait(false);

                    // For Task<T>, get the result property
                    if (targetMethod.ReturnType.IsGenericType)
                    {
                        return ((dynamic)task).Result;
                    }
                    return null;
                }

                return result;
            });
        }
    }


}
