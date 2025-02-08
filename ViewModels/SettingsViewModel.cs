using CommunityToolkit.Mvvm.ComponentModel;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        AppSettings AppSettings;

        [ObservableProperty]
        private Player playerRecord;

        [ObservableProperty]
        private string playerAvatar;

        private readonly IPinballRankingApi PinballRankingApi;

        public SettingsViewModel(IPinballRankingApi pinballRankingApi, AppSettings appSettings, ILogger<SettingsViewModel> logger) : base(logger)
        {
            AppSettings = appSettings;
            PinballRankingApi = pinballRankingApi;
        }

        public async Task LoadPlayer()
        {
            try
            {
                if (Settings.MyStatsPlayerId > 0)
                {
                    IsBusy = true;
                    var playerData = await PinballRankingApi.GetPlayer(Settings.MyStatsPlayerId);

                    PlayerRecord = playerData;
                    PlayerAvatar = PlayerRecord.ProfilePhoto != null ? PlayerRecord.ProfilePhoto?.ToString() : AppSettings.IfpaPlayerNoProfilePicUrl;
                }
                else
                {
                    PlayerRecord = null;
                    PlayerAvatar = AppSettings.IfpaPlayerNoProfilePicUrl;
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
