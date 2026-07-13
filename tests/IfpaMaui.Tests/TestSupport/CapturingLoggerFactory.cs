using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ifpa.Tests
{
    // A minimal ILoggerFactory/ILogger that records every log entry, so tests can assert on what
    // the code under test logged (e.g. the cache provider's corruption-recovery warning).
    internal sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }

        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<(LogLevel, string)> _entries;

            public CapturingLogger(ConcurrentQueue<(LogLevel, string)> entries) => _entries = entries;

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
                => _entries.Enqueue((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
