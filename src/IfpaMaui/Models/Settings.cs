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

        // The calendar filter is also mirrored into the app group, where the
        // iOS calendar widget reads it. Keys match CalendarFilter in the
        // NativeIFPA project.
        private const string WidgetCalendarLocation = "CalendarLocation";
        private const string WidgetCalendarDistance = "CalendarDistance";
        private const string WidgetCalendarRankingSystem = "CalendarRankingSystem";
        private const string WidgetCalendarShowLeagues = "CalendarShowLeagues";

        public static string LastCalendarLocation
        {
            get => Preferences.Get(nameof(LastCalendarLocation), "Chicago, Il");
            set
            {
                Preferences.Set(nameof(LastCalendarLocation), value);
                Preferences.Set(WidgetCalendarLocation, value, groupName);
            }
        }

        public static int LastCalendarDistance
        {
            get => Preferences.Get(nameof(LastCalendarDistance), 150);
            set
            {
                Preferences.Set(nameof(LastCalendarDistance), value);
                Preferences.Set(WidgetCalendarDistance, value, groupName);
            }
        }

        public static string CalendarRankingSystem
        {
            get => Preferences.Get(nameof(CalendarRankingSystem), "All");
            set
            {
                Preferences.Set(nameof(CalendarRankingSystem), value);
                Preferences.Set(WidgetCalendarRankingSystem, value, groupName);
            }
        }

        public static bool CalendarShowLeagues
        {
            get => Preferences.Get(nameof(CalendarShowLeagues), false);
            set
            {
                Preferences.Set(nameof(CalendarShowLeagues), value);
                Preferences.Set(WidgetCalendarShowLeagues, value, groupName);
            }
        }

        /// <summary>
        /// Copies the calendar filter into the app group. The setters keep it
        /// current, and this covers a filter that was saved before the widget
        /// existed.
        /// </summary>
        public static void SyncCalendarFilterToAppGroup()
        {
            Preferences.Set(WidgetCalendarLocation, LastCalendarLocation, groupName);
            Preferences.Set(WidgetCalendarDistance, LastCalendarDistance, groupName);
            Preferences.Set(WidgetCalendarRankingSystem, CalendarRankingSystem, groupName);
            Preferences.Set(WidgetCalendarShowLeagues, CalendarShowLeagues, groupName);
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
