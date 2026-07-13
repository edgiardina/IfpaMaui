using Ifpa.Caching;
using Microsoft.Extensions.Logging;
using Polly.Caching;
using SQLite;
using Xunit;

namespace Ifpa.Tests
{
    // Exercises the real SQLiteCacheProvider against an on-disk SQLite database. The provider is
    // the app's Polly cache backing store; these tests cover its round-trip, expiry and TTL-cap
    // behaviour, plus the corruption self-healing that this file was written to guarantee.
    public sealed class SQLiteCacheProviderTests : IDisposable
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"ifpa-cache-test-{Guid.NewGuid():N}.db3");

        private SQLiteCacheProvider<string> NewProvider(ILoggerFactory loggerFactory = null)
            => new SQLiteCacheProvider<string>(_dbPath, loggerFactory);

        private static Ttl Ttl(TimeSpan span) => new Ttl(span);

        public void Dispose()
        {
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                var path = _dbPath + suffix;
                try { if (File.Exists(path)) File.Delete(path); }
                catch { /* best effort cleanup */ }
            }
        }

        [Fact]
        public async Task Put_then_TryGet_returns_the_stored_value()
        {
            await using var provider = NewProvider();

            await provider.PutAsync("k1", "hello", Ttl(TimeSpan.FromMinutes(5)), CancellationToken.None, false);
            var (found, value) = await provider.TryGetAsync("k1", CancellationToken.None, false);

            Assert.True(found);
            Assert.Equal("hello", value);
        }

        [Fact]
        public async Task TryGet_for_a_missing_key_returns_false()
        {
            await using var provider = NewProvider();

            var (found, value) = await provider.TryGetAsync("does-not-exist", CancellationToken.None, false);

            Assert.False(found);
            Assert.Null(value);
        }

        [Fact]
        public async Task Expired_entries_are_not_returned()
        {
            await using var provider = NewProvider();

            await provider.PutAsync("k", "v", Ttl(TimeSpan.FromMilliseconds(50)), CancellationToken.None, false);
            await Task.Delay(250);
            var (found, _) = await provider.TryGetAsync("k", CancellationToken.None, false);

            Assert.False(found);
        }

        [Fact]
        public async Task RemoveAsync_deletes_the_entry()
        {
            await using var provider = NewProvider();
            await provider.PutAsync("k", "v", Ttl(TimeSpan.FromMinutes(5)), CancellationToken.None, false);

            await provider.RemoveAsync("k");

            var (found, _) = await provider.TryGetAsync("k", CancellationToken.None, false);
            Assert.False(found);
        }

        [Fact]
        public async Task ClearCache_removes_every_entry()
        {
            await using var provider = NewProvider();
            await provider.PutAsync("a", "1", Ttl(TimeSpan.FromMinutes(5)), CancellationToken.None, false);
            await provider.PutAsync("b", "2", Ttl(TimeSpan.FromMinutes(5)), CancellationToken.None, false);

            await provider.ClearCache();

            Assert.False((await provider.TryGetAsync("a", CancellationToken.None, false)).Item1);
            Assert.False((await provider.TryGetAsync("b", CancellationToken.None, false)).Item1);
        }

        [Fact]
        public async Task PutAsync_caps_the_ttl_at_Settings_CacheDuration()
        {
            // Request a TTL far beyond the 30-day cap; the persisted expiration must be clamped.
            await using (var provider = NewProvider())
            {
                await provider.PutAsync("k", "v", Ttl(TimeSpan.FromDays(3650)), CancellationToken.None, false);
            }

            // Re-open the file with a raw connection and inspect the stored expiration directly.
            var inspector = new SQLiteAsyncConnection(_dbPath);
            try
            {
                var item = await inspector.FindAsync<CacheItem>("k");

                Assert.NotNull(item);
                Assert.True(item.Expiration <= DateTime.UtcNow.Add(Ifpa.Models.Settings.CacheDuration).AddMinutes(1),
                    $"Expiration {item.Expiration:o} should have been capped to ~30 days out.");
                Assert.True(item.Expiration > DateTime.UtcNow.AddDays(29),
                    "Expiration should still be near the 30-day cap, not truncated to something small.");
            }
            finally
            {
                await inspector.CloseAsync();
            }
        }

        [Fact]
        public async Task Constructing_over_a_corrupt_database_file_self_heals()
        {
            // The scenario this provider guards against: the cache file on disk is corrupt / not a
            // SQLite database at all. Construction must transparently rebuild it instead of throwing
            // (which previously bricked every cached API call).
            File.WriteAllBytes(_dbPath, System.Text.Encoding.UTF8.GetBytes(
                new string('X', 4096) + "this is not a valid sqlite database"));

            var loggerFactory = new CapturingLoggerFactory();

            await using var provider = NewProvider(loggerFactory); // must not throw

            // The rebuilt database must be fully usable.
            await provider.PutAsync("k", "v", Ttl(TimeSpan.FromMinutes(5)), CancellationToken.None, false);
            var (found, value) = await provider.TryGetAsync("k", CancellationToken.None, false);
            Assert.True(found);
            Assert.Equal("v", value);

            // And the recovery path should have logged a warning about the corrupt cache.
            Assert.Contains(loggerFactory.Entries, e =>
                e.Level == LogLevel.Warning &&
                e.Message.Contains("corrupt", StringComparison.OrdinalIgnoreCase));
        }
    }
}
