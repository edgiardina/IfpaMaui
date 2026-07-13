using Ifpa.Models.Database;
using SQLite;

namespace Ifpa.Services
{
    public class LocalDatabase
    {
        readonly SQLiteAsyncConnection database;
        public LocalDatabase(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            database.CreateTableAsync<ActivityFeedItem>().Wait();
            database.CreateTableAsync<Favorite>().Wait();
        }

        public Task<List<ActivityFeedItem>> GetActivityFeedRecords()
        {
            return database.Table<ActivityFeedItem>().OrderByDescending(n => n.CreatedDateTime).Take(100).ToListAsync();
        }

        public async Task<int> CreateActivityFeedRecord(ActivityFeedItem item)
        {
            return await database.InsertAsync(item);
        }

        public async Task UpdateActivityFeedRecord(ActivityFeedItem item)
        {
            if(item.ID > 0)
            {
                await database.UpdateAsync(item);
            }
        }

        public async Task ClearActivityFeed()
        {
            await database.DeleteAllAsync<ActivityFeedItem>();
        }

        public async Task<int> GetUnreadActivityCount()
        {
            return await database.Table<ActivityFeedItem>().CountAsync(n => !n.HasBeenSeen);
        }

        public async Task<IEnumerable<int>> ParseNewTournaments(IEnumerable<int> tournamentIds)
        {
            var tournamentsSeen = await database.Table<ActivityFeedItem>().Where(n => n.ActivityType == ActivityFeedType.TournamentResult).ToListAsync();
            return tournamentIds.Except(tournamentsSeen.Select(n => n.RecordID.Value));
        }

        public async Task<int> AddFavorite(int playerId)
        {
            return await database.InsertAsync(new Favorite { PlayerID = playerId, CreatedDateTime = DateTime.Now });
        }

        public Task<List<Favorite>> GetFavorites()
        {
            return database.Table<Favorite>().OrderByDescending(n => n.CreatedDateTime).ToListAsync();
        }

        public async Task RemoveFavorite(int playerId)
        {
            await database.Table<Favorite>().DeleteAsync(n => n.PlayerID == playerId);
        }

        public async Task<bool> HasFavorite(int playerId)
        {
            return (await database.Table<Favorite>().CountAsync(n => n.PlayerID == playerId)) == 1;
        }
    }
}
