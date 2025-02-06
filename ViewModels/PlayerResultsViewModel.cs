using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal.Players;
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
        public PlayerRankingSystem RankingType { get; set; }

        public PlayerRankingSystem[] RankingTypeOptions => new[] { PlayerRankingSystem.Main, PlayerRankingSystem.Women, PlayerRankingSystem.Youth };

        public int PlayerId { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;


        public PlayerResultsViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerResultsViewModel> logger) : base(logger)
        {
            State = ResultType.Active;
            RankingType = PlayerRankingSystem.Main;
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var player = await PinballRankingApi.GetPlayer(PlayerId);

                var activeResults = await PinballRankingApi.GetPlayerResults(PlayerId, RankingType, ResultType.Active);
                if (activeResults.ResultsCount > 0)
                {
                    ActiveResults = activeResults.Results.ToObservableCollection();
                }

                var unusedResults = await PinballRankingApi.GetPlayerResults(PlayerId, RankingType, ResultType.NonActive);
                if (unusedResults.ResultsCount > 0)
                {
                    UnusedResults = unusedResults.Results.ToObservableCollection();
                }

                var pastResults = await PinballRankingApi.GetPlayerResults(PlayerId, RankingType, ResultType.Inactive);
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