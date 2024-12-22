using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.ViewModels
{
    public partial class CustomRankingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<CustomRankingView> customRankings = new List<CustomRankingView>();

        [ObservableProperty]
        private bool isPopulated;

        [ObservableProperty]
        private CustomRankingView selectedCustomRanking;

        private bool dataNotLoaded = true;

        public CustomRankingsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<CustomRankingsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Custom Rankings";
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            dataNotLoaded = false;

            try
            {
                var tempList = await PinballRankingApiV2.GetRankingCustomViewList();

                CustomRankings = tempList.CustomRankingView;

                IsPopulated = CustomRankings.Count > 0 || dataNotLoaded;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading custom rankings");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewCustomRankingDetail()
        {
            await Shell.Current.GoToAsync($"custom-ranking-details?viewId={SelectedCustomRanking.ViewId}");
            SelectedCustomRanking = null;
        }
    }

}