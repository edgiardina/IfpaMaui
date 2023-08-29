using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.v2.WPPR;
using PinballApi.Models.WPPR.v2.Players;
using System.Diagnostics;

namespace Ifpa.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        AppSettings AppSettings;
        
        private Player playerRecord = new Player { PlayerStats = new PinballApi.Models.WPPR.v1.Players.PlayerStats { }, ChampionshipSeries = new List<ChampionshipSeries> { } };

        public string PlayerAvatar
        {
            get
            {
                if (PlayerRecord.ProfilePhoto != null)
                    return PlayerRecord.ProfilePhoto?.ToString();
                else
                    return AppSettings.IfpaPlayerNoProfilePicUrl;
            }
        }

        public SettingsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, ILogger<SettingsViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            AppSettings = appSettings;
        }

        public async Task LoadPlayer()
        {
            try
            {
                if (Settings.MyStatsPlayerId > 0)
                {
                    IsBusy = true;
                    var playerData = await PinballRankingApiV2.GetPlayer(Settings.MyStatsPlayerId);              

                    PlayerRecord = playerData;               
                }
                else
                {
                    playerRecord = new Player { PlayerStats = new PinballApi.Models.WPPR.v1.Players.PlayerStats { }, ChampionshipSeries = new List<ChampionshipSeries> { } };
                    OnPropertyChanged(null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading player id {0} in settings", Settings.MyStatsPlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public Player PlayerRecord
        {
            get { return playerRecord; }
            set
            {
                playerRecord = value;
                OnPropertyChanged(null);
            }
        }

        public string Name => PlayerRecord.FirstName != null || PlayerRecord.LastName != null ? PlayerRecord.FirstName + " " + PlayerRecord.LastName : null;

        public bool NotifyOnRankChange
        {
            get => Settings.NotifyOnRankChange;
            set
            {
                Settings.NotifyOnRankChange = value;
                OnPropertyChanged(nameof(NotifyOnRankChange));
            }
        }
        public bool NotifyOnTournamentResult
        {
            get => Settings.NotifyOnTournamentResult;
            set
            {
                Settings.NotifyOnTournamentResult = value;
                OnPropertyChanged(nameof(NotifyOnTournamentResult));
            }
        }

        public bool NotifyOnNewBlogPost
        {
            get => Settings.NotifyOnNewBlogPost;
            set
            {
                Settings.NotifyOnNewBlogPost = value;
                OnPropertyChanged(nameof(NotifyOnNewBlogPost));
            }
        }

        public bool NotifyOnNewCalendarEntry
        {
            get => Settings.NotifyOnNewCalendarEntry;
            set
            {
                Settings.NotifyOnNewCalendarEntry = value;
                OnPropertyChanged(nameof(NotifyOnNewCalendarEntry));
            }
        }
    }
}
