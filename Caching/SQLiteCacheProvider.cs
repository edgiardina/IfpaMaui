using Ifpa.Models;
using Polly.Caching;
using SQLite;
using System.Text.Json;

namespace Ifpa.Caching
{
    public class SQLiteCacheProvider<T> : IAsyncCacheProvider<T>
    {
        private readonly SQLiteAsyncConnection _db;

        public SQLiteCacheProvider(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            Task.Run(() => _db.CreateTableAsync<CacheItem>()).Wait();
            Task.Run(CleanupExpiredItems).Wait(); // Remove expired items on initialization
        }

        private async Task CleanupExpiredItems()
        {
            await _db.Table<CacheItem>().Where(item => item.Expiration <= DateTime.UtcNow).DeleteAsync();
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _db.DeleteAsync<CacheItem>(key);
        }

        public async Task<(bool, T?)> TryGetAsync(string key, CancellationToken cancellationToken, bool continueOnCapturedContext)
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
        }

        public async Task PutAsync(string key, T? value, Ttl ttl, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            if (ttl.Timespan > Settings.CacheDuration)
                ttl.Timespan = Settings.CacheDuration;

            var item = new CacheItem
            {
                Id = key,
                Value = JsonSerializer.Serialize(value),
                Expiration = DateTime.UtcNow.Add(ttl.Timespan)
            };

            await _db.InsertOrReplaceAsync(item);
            await CleanupExpiredItems(); // Enforce cleanup after insertion
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
