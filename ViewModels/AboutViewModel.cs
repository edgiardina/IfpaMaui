using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Ifpa.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        AppSettings AppSettings { get; set; }

        public ObservableCollection<Player> Sponsors { get; set; }

        public int CreatorIfpaNumber => 16927;

        public AboutViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, ILogger<AboutViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            AppSettings = appSettings;
            Sponsors = new ObservableCollection<Player>();
        }

        public async Task LoadSponsors()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Sponsors.Clear();

                var tempList = await PinballRankingApiV2.GetPlayers(AppSettings.Sponsors);

                foreach (var player in tempList.OrderBy(i => i.PlayerStats.CurrentWpprRank))
                {
                    Sponsors.Add(player);
                }

                OnPropertyChanged("Sponsors");
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

        public string CurrentVersion => VersionTracking.CurrentVersion;

        public string MinorVersion => VersionTracking.CurrentBuild;
    }
}