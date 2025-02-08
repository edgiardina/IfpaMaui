using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Rankings.Custom;

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

        private readonly IPinballRankingApi PinballRankingApi;

        public CustomRankingsViewModel(IPinballRankingApi pinballRankingApi, ILogger<CustomRankingsViewModel> logger) : base(logger)
        {
            Title = "Custom Rankings";
            PinballRankingApi = pinballRankingApi;
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
                CustomRankings = await PinballRankingApi.GetCustomRankings();

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