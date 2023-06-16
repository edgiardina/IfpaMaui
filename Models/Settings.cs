﻿using Ifpa.Services;
using PinballApi.Models.WPPR.v1.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ifpa.Models
{
    public static class Settings
    {
        static LocalDatabase localDatabase;

        //Groupname is so we can share the player ID with the upcoming iOS Widget. 
        private const string groupName = "group.com.edgiardina.ifpa";

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
            get => Preferences.Get("CurrentTabIndex", 0);
            set => Preferences.Set("CurrentTabIndex", value);
        }

        public static bool NotifyOnRankChange
        {
            get => Preferences.Get("NotifyOnRankChange", true);
            set => Preferences.Set("NotifyOnRankChange", value);
        }
        public static bool NotifyOnTournamentResult
        {
            get => Preferences.Get("NotifyOnTournamentResult", true);
            set => Preferences.Set("NotifyOnTournamentResult", value);
        }

        public static bool NotifyOnNewBlogPost
        {
            get => Preferences.Get("NotifyOnNewBlogPost", false);
            set => Preferences.Set("NotifyOnNewBlogPost", value);
        }

        public static int LastBlogPostGuid
        {
            get => Preferences.Get("LastBlogPostGuid", 0);
            set => Preferences.Set("LastBlogPostGuid", value);
        }

        public static bool HasConfiguredMyStats
        {
            get => MyStatsPlayerId != 0;
        }

        public static int MyStatsPlayerId
        {
            get
            {
                var playerIdGroup = Preferences.Get("PlayerId", 0, groupName);
                var playerId = Preferences.Get("PlayerId", 0);
                if (playerId != 0 && playerIdGroup == 0)
                {
                    Preferences.Set("PlayerId", playerId, groupName);
                }
                return Preferences.Get("PlayerId", 0);
            }
            private set
            {
                Preferences.Set("PlayerId", value);
                //Save to group for Widget access
                Preferences.Set("PlayerId", value, groupName);
            }
        }

        public static int MyStatsCurrentWpprRank
        {
            get => Preferences.Get("CurrentWpprRank", 0);
            set => Preferences.Set("CurrentWpprRank", value);
        }

        public static async Task SetMyStatsPlayer(int playerId, int currentWpprRank)
        {
            //Clear Activity Log as we are switching players
            await Settings.LocalDatabase.ClearActivityFeed();

            MyStatsPlayerId = playerId;
            MyStatsCurrentWpprRank = currentWpprRank;
        }

        public static async Task<IEnumerable<int>> FindUnseenTournaments(IList<Result> results)
        {
            return await LocalDatabase.ParseNewTournaments(results.Select(n => n.TournamentId));
        }

    }
}
