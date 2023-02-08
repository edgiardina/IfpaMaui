using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        AppSettings AppSettings { get; set; }

        public AboutViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            AppSettings = appSettings;
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
    }
}