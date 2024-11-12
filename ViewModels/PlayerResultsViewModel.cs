using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi.Models.WPPR.v2.Tournaments;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public class PlayerResultsViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerResult> ActiveResults { get; set; }
        public ObservableCollection<PlayerResult> UnusedResults { get; set; }
        public ObservableCollection<PlayerResult> PastResults { get; set; }

        public Command LoadItemsCommand { get; set; }

        public ResultType State { get; set; }
        public RankingType RankingType { get; set; }

        public RankingType[] RankingTypeOptions => new[] { RankingType.Main, RankingType.Women, RankingType.Youth };
     

        public int PlayerId { get; set; }

        public PlayerResultsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerResultsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Results";
            State = ResultType.Active;
            RankingType = RankingType.Main;
            ActiveResults = new ObservableCollection<PlayerResult>();
            UnusedResults = new ObservableCollection<PlayerResult>();
            PastResults = new ObservableCollection<PlayerResult>();

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var player = await PinballRankingApiV2.GetPlayer(PlayerId);

                ActiveResults.Clear();
                UnusedResults.Clear();
                PastResults.Clear();

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

                OnPropertyChanged(nameof(PastResults));
                OnPropertyChanged(nameof(UnusedResults));
                OnPropertyChanged(nameof(ActiveResults));
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
    }
}