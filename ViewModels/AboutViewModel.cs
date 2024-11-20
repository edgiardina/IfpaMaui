using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.ViewModels
{
    public partial class AboutViewModel : BaseViewModel
    {
        private AppSettings AppSettings { get; set; }

        [ObservableProperty]
        private List<Player> sponsors = new List<Player>();

        public string CurrentVersion => VersionTracking.CurrentVersion;

        public string MinorVersion => VersionTracking.CurrentBuild;

        public int CreatorIfpaNumber => 16927;

        public AboutViewModel(PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, ILogger<AboutViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            AppSettings = appSettings;
        }

        [RelayCommand]
        public async Task LoadSponsors()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Sponsors.Clear();

                var tempList = await PinballRankingApiV2.GetPlayers(AppSettings.Sponsors);

                Sponsors = tempList.OrderBy(i => i.PlayerStats.CurrentWpprRank).ToList();

                logger.LogDebug("Loaded {0} sponsors", Sponsors.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading sponsors");
            }
            finally
            {
                IsBusy = false;
            }
        }

        //TODO: when it's ported to MAUI, use store review plugin
        [RelayCommand]
        public async Task OpenReview()
        {
            var url = string.Empty;

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            {
                url = $"itms-apps://itunes.apple.com/WebObjects/MZStore.woa/wa/viewContentsUserReviews?id={AppSettings.AppStoreAppId}&amp;onlyLatestVersion=true&amp;pageNumber=0&amp;sortOrdering=1&amp;type=Purple+Software";
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                url = $"https://play.google.com/store/apps/details?id={AppSettings.PlayStoreAppId}";
            }

            if (string.IsNullOrWhiteSpace(url))
                return;

            await Browser.OpenAsync(url, BrowserLaunchMode.External);
        }

        [RelayCommand]
        public async Task LearnMore()
        {
            await Browser.OpenAsync("http://tiltforums.com/t/ifpa-app-now-available-on-the-app-store/4543", BrowserLaunchMode.External);
        }

        [RelayCommand]
        public async Task ViewPlayer(int playerId)
        {
            await Shell.Current.GoToAsync($"player-details?playerId={playerId}");
        }

        [RelayCommand]
        public async Task Flagpedia()
        {
            await Browser.OpenAsync("https://flagpedia.net/", BrowserLaunchMode.External);
        }

    }
}