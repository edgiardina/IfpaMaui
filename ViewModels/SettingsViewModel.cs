using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Caching;
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

        [ObservableProperty]
        private string cacheSize;

        private readonly IPinballRankingApi PinballRankingApi;

        public SettingsViewModel(IPinballRankingApi pinballRankingApi, AppSettings appSettings, ILogger<SettingsViewModel> logger) : base(logger)
        {
            AppSettings = appSettings;
            PinballRankingApi = pinballRankingApi;
            UpdateCacheSize();
        }

        private void UpdateCacheSize()
        {
            try
            {
                var fileInfo = new FileInfo(Settings.CacheDatabasePath);
                if (fileInfo.Exists)
                {
                    var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
                    CacheSize = $"{sizeInMb:F2} MB";
                }
                else
                {
                    CacheSize = "0 MB";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating cache size");
                CacheSize = "Unknown";
            }
        }

        [RelayCommand]
        public async Task ClearCache()
        {
            try
            {
                IsBusy = true;

                // Create a cache provider and clear it
                await using var cache = new SQLiteCacheProvider<object>(Settings.CacheDatabasePath);
                await cache.ClearCache();

                UpdateCacheSize();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing cache");
            }
            finally
            {
                IsBusy = false;
            }
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
