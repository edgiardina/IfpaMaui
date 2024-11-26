using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi.Models.WPPR.v2.Tournaments;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class PlayerResultsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<PlayerResult> activeResults = new ObservableCollection<PlayerResult>();
        
        [ObservableProperty]
        private ObservableCollection<PlayerResult> unusedResults = new ObservableCollection<PlayerResult>();
        
        [ObservableProperty]
        private ObservableCollection<PlayerResult> pastResults = new ObservableCollection<PlayerResult>();

        [ObservableProperty]
        private PlayerResult selectedResult;

        public ResultType State { get; set; }
        public RankingType RankingType { get; set; }

        public RankingType[] RankingTypeOptions => new[] { RankingType.Main, RankingType.Women, RankingType.Youth };

        public int PlayerId { get; set; }

        public PlayerResultsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerResultsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            State = ResultType.Active;
            RankingType = RankingType.Main;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var player = await PinballRankingApiV2.GetPlayer(PlayerId);

                var activeResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.Active);
                if (activeResults.ResultsCount > 0)
                {
                    ActiveResults = activeResults.Results.ToObservableCollection();
                }

                var unusedResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.NonActive);
                if (unusedResults.ResultsCount > 0)
                {
                    UnusedResults = unusedResults.Results.ToObservableCollection();
                }

                var pastResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.Inactive);
                if (pastResults.ResultsCount > 0)
                {
                    PastResults = pastResults.Results.ToObservableCollection();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading player results id {0}", PlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewTournamentResults()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedResult.TournamentId}");
            SelectedResult = null;
        }
    }
}