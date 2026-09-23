using CommunityToolkit.Mvvm.Messaging;
using Ifpa.Services;
using PinballApi.Models.WPPR.Universal.Players;

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

        // The calendar filter lives in the app group, so the iOS calendar
        // widget reads the same values as the Calendar tab. The key names
        // match CalendarFilter in the NativeIFPA project.
        public static string LastCalendarLocation
        {
            get => Preferences.Default.Get(nameof(LastCalendarLocation), "Chicago, Il", groupName);
            set => Preferences.Default.Set(nameof(LastCalendarLocation), value, groupName);
        }

        public static int LastCalendarDistance
        {
            get => Preferences.Default.Get(nameof(LastCalendarDistance), 150, groupName);
            set => Preferences.Default.Set(nameof(LastCalendarDistance), value, groupName);
        }

        public static string CalendarRankingSystem
        {
            get => Preferences.Default.Get(nameof(CalendarRankingSystem), "All", groupName);
            set => Preferences.Default.Set(nameof(CalendarRankingSystem), value, groupName);
        }

        public static bool CalendarShowLeagues
        {
            get => Preferences.Default.Get(nameof(CalendarShowLeagues), false, groupName);
            set => Preferences.Default.Set(nameof(CalendarShowLeagues), value, groupName);
        }

        /// <summary>
        /// Earlier versions kept the calendar filter in the app's own store.
        /// Move it to the app group once, and remove the old copy. Runs at
        /// every process start, so the filter has moved before the Calendar
        /// tab, the notification job or the widget reads it.
        /// </summary>
        public static void MoveCalendarFilterToAppGroup()
        {
            Move(nameof(LastCalendarLocation), "Chicago, Il");
            Move(nameof(LastCalendarDistance), 150);
            Move(nameof(CalendarRankingSystem), "All");
            Move(nameof(CalendarShowLeagues), false);

            static void Move<T>(string key, T defaultValue)
            {
                if (!Preferences.Default.ContainsKey(key))
                    return;

                Preferences.Default.Set(key, Preferences.Default.Get(key, defaultValue), groupName);
                Preferences.Default.Remove(key);
            }
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
                // Always prefer App Group
                var playerIdGroup = Preferences.Get(PLAYER_ID, 0, groupName);
                if (playerIdGroup != 0)
                    return playerIdGroup;

                // Fall back to app's own store
                var playerId = Preferences.Get(PLAYER_ID, 0);
                if (playerId != 0)
                {
                    // Sync into App Group for widget
                    Preferences.Set(PLAYER_ID, playerId, groupName);
                }
                return playerId;
            }
            private set
            {
                // Always write to both
                Preferences.Set(PLAYER_ID, value);
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

            WeakReferenceMessenger.Default.Send(new MyStatsPlayerChangedMessage());
        }

        public static async Task<IEnumerable<int>> FindUnseenTournaments(IList<PlayerResult> results)
        {
            return await LocalDatabase.ParseNewTournaments(results.Select(n => n.TournamentId));
        }

        public static readonly string LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "logs", "log-.txt");

    }
}
