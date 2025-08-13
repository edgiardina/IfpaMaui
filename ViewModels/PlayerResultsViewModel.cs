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
        private ObservableCollection<PlayerRankingSystem> rankingTypeOptions = new ObservableCollection<PlayerRankingSystem>();

        [ObservableProperty]
        private PlayerResult selectedResult;

        public ResultType State { get; set; }
        public PlayerRankingSystem RankingType { get; set; }

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
               
                RankingTypeOptions = player.PlayerStats.System.Select(s => s.System).ToObservableCollection();

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

        [RelayCommand]
        public async Task RankingProfileSelect()
        {
            string action = await Shell.Current.DisplayActionSheet(Strings.PlayerResultsPage_RankingProfile, Strings.Cancel, null, RankingTypeOptions.Select(a => a.ToString()).ToArray());

            if (action != null && action != Strings.Cancel)
            {
                RankingType = (PlayerRankingSystem)Enum.Parse(typeof(PlayerRankingSystem), action);
                _ = LoadItems();
            }
        }
    }
}