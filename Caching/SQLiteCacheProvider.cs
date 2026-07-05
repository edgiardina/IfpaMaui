using Ifpa.Models;
using Microsoft.Extensions.Logging;
using Polly.Caching;
using SQLite;
using System.Text.Json;

namespace Ifpa.Caching
{
    public class SQLiteCacheProvider<T> : IAsyncCacheProvider<T>, IAsyncDisposable
    {
        private readonly string _dbPath;
        private readonly ILogger _logger;
        private SQLiteAsyncConnection _db;

        public SQLiteCacheProvider(string dbPath, ILogger logger = null)
        {
            _dbPath = dbPath;
            _logger = logger;
            _db = new SQLiteAsyncConnection(dbPath);
            // Initialize synchronously so a usable connection is guaranteed before first use.
            // A corrupt cache file self-heals here rather than throwing and bricking every cached call.
            Task.Run(InitializeAsync).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _db.CreateTableAsync<CacheItem>();
                await CleanupExpiredItems(); // Remove expired items on initialization
            }
            catch (Exception ex) when (IsCorruptionException(ex))
            {
                _logger?.LogWarning(ex,
                    "Cache database at {Path} is corrupt; deleting and recreating it. Cached API data will be re-fetched; no user data is affected.",
                    _dbPath);
                await RecreateDatabaseAsync();
            }
        }

        private async Task CleanupExpiredItems()
        {
            await _db.Table<CacheItem>().Where(item => item.Expiration <= DateTime.UtcNow).DeleteAsync();
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await RunWithRecovery(() => _db.DeleteAsync<CacheItem>(key));
        }

        public Task<(bool, T)> TryGetAsync(string key, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            return RunWithRecovery(async () =>
            {
                await CleanupExpiredItems(); // Clean expired items before fetching
                var item = await _db.FindAsync<CacheItem>(key);

                if (item != null && item.Expiration > DateTime.UtcNow)
                {
                    // Deserialize to an object since type is not known at compile time
                    var value = JsonSerializer.Deserialize<T>(item.Value);
                    return (true, value);
                }

                return (false, default(T)); // Return false and null if no valid cache item is found
            });
        }

        public Task PutAsync(string key, T value, Ttl ttl, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            if (ttl.Timespan > Settings.CacheDuration)
                ttl.Timespan = Settings.CacheDuration;

            var item = new CacheItem
            {
                Id = key,
                Value = JsonSerializer.Serialize(value),
                Expiration = DateTime.UtcNow.Add(ttl.Timespan)
            };

            return RunWithRecovery(async () =>
            {
                await _db.InsertOrReplaceAsync(item);
                await CleanupExpiredItems(); // Enforce cleanup after insertion
            });
        }

        public async Task ClearCache()
        {
            // Delete all entries
            await _db.DeleteAllAsync<CacheItem>();
            // Run VACUUM to reclaim space and optimize the database
            await _db.ExecuteAsync("VACUUM");
        }

        public async ValueTask DisposeAsync()
        {
            await _db.CloseAsync();
        }

        // Runs a cache operation, transparently rebuilding the database once if it is found to be
        // corrupt. The cache holds only re-fetchable API responses, so discarding it is always safe.
        private async Task RunWithRecovery(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception ex) when (IsCorruptionException(ex))
            {
                _logger?.LogWarning(ex,
                    "Cache database at {Path} is corrupt; rebuilding and retrying. Cached API data will be re-fetched; no user data is affected.",
                    _dbPath);
                await RecreateDatabaseAsync();
                await operation();
            }
        }

        private async Task<TResult> RunWithRecovery<TResult>(Func<Task<TResult>> operation)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsCorruptionException(ex))
            {
                _logger?.LogWarning(ex,
                    "Cache database at {Path} is corrupt; rebuilding and retrying. Cached API data will be re-fetched; no user data is affected.",
                    _dbPath);
                await RecreateDatabaseAsync();
                return await operation();
            }
        }

        private async Task RecreateDatabaseAsync()
        {
            // Release our handle so the underlying file can be deleted.
            try { await _db.CloseAsync(); }
            catch (Exception ex) { _logger?.LogDebug(ex, "Error closing corrupt cache connection (ignored)"); }

            DeleteDatabaseFiles();

            // Fresh, empty database with the schema in place.
            _db = new SQLiteAsyncConnection(_dbPath);
            await _db.CreateTableAsync<CacheItem>();
        }

        private void DeleteDatabaseFiles()
        {
            // SQLite may keep sidecar files (WAL / shared-memory / rollback journal); remove them all.
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                var path = _dbPath + suffix;
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not delete corrupt cache file {Path}", path);
                }
            }
        }

        private static bool IsCorruptionException(Exception ex)
        {
            switch (ex)
            {
                case SQLiteException sqlite:
                    // SQLITE_CORRUPT ("database disk image is malformed") or SQLITE_NOTADB.
                    return sqlite.Result == SQLite3.Result.Corrupt
                        || sqlite.Result == SQLite3.Result.NonDBFile;
                case AggregateException aggregate:
                    // .Wait()/.GetResult() wrap the SQLiteException; unwrap and inspect.
                    return aggregate.Flatten().InnerExceptions.Any(IsCorruptionException);
                default:
                    return ex.InnerException != null && IsCorruptionException(ex.InnerException);
            }
        }
    }

    public class CacheItem
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Value { get; set; }
        public DateTime Expiration { get; set; }
    }
}
