using Ifpa.Services;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.Models
{
    public static class Settings
    {
        private static string PLAYER_ID = "PlayerId";
        private static string CURRENT_WPPR_RANK = "CurrentWpprRank";

        static LocalDatabase localDatabase;

        //Groupname is so we can share the player ID with the upcoming iOS Widget. 
        private const string groupName = "group.com.edgiardina.ifpa";


        public static string CacheDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "app_cache.db3");
        public static TimeSpan CacheDuration = TimeSpan.FromDays(30);
        public static LocalDatabase LocalDatabase
        {
            get
            {
                if (localDatabase == null)
                {
                    localDatabase = new LocalDatabase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ActivityFeedSQLite.db3"));
                }
                return localDatabase;
            }
        }

        public static int CurrentTabIndex
        {
            get => Preferences.Get(nameof(CurrentTabIndex), 0);
            set => Preferences.Set(nameof(CurrentTabIndex), value);
        }

        public static bool NotifyOnRankChange
        {
            get => Preferences.Get(nameof(NotifyOnRankChange), true);
            set => Preferences.Set(nameof(NotifyOnRankChange), value);
        }
        public static bool NotifyOnTournamentResult
        {
            get => Preferences.Get(nameof(NotifyOnTournamentResult), true);
            set => Preferences.Set(nameof(NotifyOnTournamentResult), value);
        }

        public static bool NotifyOnNewBlogPost
        {
            get => Preferences.Get(nameof(NotifyOnNewBlogPost), false);
            set => Preferences.Set(nameof(NotifyOnNewBlogPost), value);
        }

        public static bool NotifyOnNewCalendarEntry
        {
            get => Preferences.Get(nameof(NotifyOnNewCalendarEntry), false);
            set => Preferences.Set(nameof(NotifyOnNewCalendarEntry), value);
        }

        public static int LastBlogPostGuid
        {
            get => Preferences.Get(nameof(LastBlogPostGuid), 0);
            set => Preferences.Set(nameof(LastBlogPostGuid), value);
        }

        public static string LastCalendarLocation
        {
            get => Preferences.Get(nameof(LastCalendarLocation), "Chicago, Il");
            set => Preferences.Set(nameof(LastCalendarLocation), value);
        }

        public static int LastCalendarDistance
        {
            get => Preferences.Get(nameof(LastCalendarDistance), 150);
            set => Preferences.Set(nameof(LastCalendarDistance), value);
        }

        public static string CalendarRankingSystem
        {
            get => Preferences.Get(nameof(CalendarRankingSystem), "All");
            set => Preferences.Set(nameof(CalendarRankingSystem), value);
        }

        public static bool CalendarShowLeagues
        {
            get => Preferences.Get(nameof(CalendarShowLeagues), false);
            set => Preferences.Set(nameof(CalendarShowLeagues), value);
        }

        public static long LastCalendarIdSeen
        {
            get => Preferences.Get(nameof(LastCalendarIdSeen), 0L);
            set => Preferences.Set(nameof(LastCalendarIdSeen), value);
        }

        public static bool HasConfiguredMyStats
        {
            get => MyStatsPlayerId != 0;
        }

        public static int MyStatsPlayerId
        {
            get
            {
                var playerIdGroup = Preferences.Get(PLAYER_ID, 0, groupName);
                var playerId = Preferences.Get(PLAYER_ID, 0);
                if (playerId != 0 && playerIdGroup == 0)
                {
                    Preferences.Set(PLAYER_ID, playerId, groupName);
                }
                return Preferences.Get(PLAYER_ID, 0);
            }
            private set
            {
                Preferences.Set(PLAYER_ID, value);
                //Save to group for Widget access
                Preferences.Set(PLAYER_ID, value, groupName);
            }
        }

        public static int MyStatsCurrentWpprRank
        {
            get => Preferences.Get(CURRENT_WPPR_RANK, 0);
            set => Preferences.Set(CURRENT_WPPR_RANK, value);
        }

        public static async Task SetMyStatsPlayer(int playerId, int currentWpprRank)
        {
            //Clear Activity Log as we are switching players
            await LocalDatabase.ClearActivityFeed();

            MyStatsPlayerId = playerId;
            MyStatsCurrentWpprRank = currentWpprRank;
        }

        public static async Task<IEnumerable<int>> FindUnseenTournaments(IList<PlayerResult> results)
        {
            return await LocalDatabase.ParseNewTournaments(results.Select(n => n.TournamentId));
        }

        public static readonly string LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "logs", "log-.txt");

    }
}
