using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Rankings.Custom;
using PinballApi.Models.WPPR.Universal.Tournaments;

namespace Ifpa.ViewModels
{
    public partial class CustomRankingsDetailViewModel : BaseViewModel
    {
        public int ViewId { get; internal set; }

        [ObservableProperty]
        private List<ViewResult> viewResults = new List<ViewResult>();

        [ObservableProperty]
        private List<CustomViewTournament> tournaments = new List<CustomViewTournament>();

        [ObservableProperty]
        private List<ViewFilter> viewFilters = new List<ViewFilter>();

        [ObservableProperty]
        private ViewResult selectedViewResult;

        [ObservableProperty]
        private Tournament selectedTournament;

        [ObservableProperty]
        private bool isPopulated;

        private bool dataNotLoaded = true;

        private readonly IPinballRankingApi PinballRankingApi;

        public CustomRankingsDetailViewModel(IPinballRankingApi pinballRankingApi, ILogger<CustomRankingsDetailViewModel> logger) : base(logger)
        {
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
                var tempList = await PinballRankingApi.GetCustomRankingViewResult(ViewId);

                Tournaments = tempList.Tournaments;
                ViewResults = tempList.ViewResults;

                // Use linq to trim the name property of the ViewFilters
                ViewFilters = tempList.ViewFilters
                                         .Select(x => new ViewFilter { Name = x.Name.Trim(), Setting = x.Setting })
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
            SelectedViewResult = null;
        }

        [RelayCommand]
        public async Task SelectTournament()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournament.TournamentId}");
            SelectedTournament = null;
        }

    }
}