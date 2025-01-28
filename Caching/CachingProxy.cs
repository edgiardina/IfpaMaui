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

        public event Action<string, object[]> CachedValueUsed;

        public void Configure(T decorated, AsyncPolicy policy, ILogger logger)
        {
            _decorated = decorated;
            _policy = policy;
            _logger = logger;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            _logger.LogInformation($"Invoking {targetMethod.Name}");

            return _policy.Execute(() =>
            {
                _logger.LogInformation($"Fetching fresh data for {targetMethod.Name}");
                return targetMethod.Invoke(_decorated, args);
            });
        }

        protected override async Task<object> InvokeAsync(MethodInfo targetMethod, object[] args)
        {
            _logger.LogInformation($"Invoking {targetMethod.Name}");

            return await _policy.ExecuteAsync(async () =>
            {
                _logger.LogInformation($"Fetching fresh data for {targetMethod.Name}");
                return await (Task<object>)targetMethod.Invoke(_decorated, args);
            });
        }
    }

}
