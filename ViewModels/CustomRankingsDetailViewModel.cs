using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.ViewModels
{
    public partial class CustomRankingsDetailViewModel : BaseViewModel
    {
        public int ViewId { get; internal set; }

        [ObservableProperty]
        private List<CustomRankingViewResult> viewResults = new List<CustomRankingViewResult>();

        [ObservableProperty]
        private List<Tournament> tournaments = new List<Tournament>();

        [ObservableProperty]
        private List<CustomRankingViewFilter> viewFilters = new List<CustomRankingViewFilter>();

        [ObservableProperty]
        private CustomRankingViewResult selectedViewResult;

        [ObservableProperty]
        private Tournament selectedTournament;

        [ObservableProperty]
        private bool isPopulated;

        private bool dataNotLoaded = true;

        public CustomRankingsDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<CustomRankingsDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {

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
                var tempList = await PinballRankingApiV2.GetRankingCustomView(ViewId);

                Tournaments = tempList.Tournaments;
                ViewResults = tempList.ViewResults;

                // Use linq to trim the name property of the ViewFilters
                ViewFilters = tempList.ViewFilters
                                         .Select(x => new CustomRankingViewFilter { Name = x.Name.Trim(), Setting = x.Setting })
                                         .ToList();

                Title = tempList.Title;
                IsPopulated = Tournaments.Count > 0 || dataNotLoaded;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading custom rankings detail for view {0}", ViewId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SelectPlayerDetails()
        {
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedViewResult.PlayerId}");
        }

        [RelayCommand]
        public async Task SelectTournament()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournament.TournamentId}"); 
        }

    }
}