using System.Diagnostics;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi.Models.WPPR.v2;
using Ifpa.Models;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class PlayerResultsViewModel : BaseViewModel
    {
        public ObservableCollectionRange<PlayerResult> ActiveResults { get; set; }
        public ObservableCollectionRange<PlayerResult> UnusedResults { get; set; }
        public ObservableCollectionRange<PlayerResult> PastResults { get; set; }

        public Command LoadItemsCommand { get; set; }

        public ResultType State { get; set; }
        public RankingType RankingType { get; set; }

        public RankingType[] RankingTypeOptions => new[] { RankingType.Main, RankingType.Women, RankingType.Youth };
     
        public bool ShowRankingTypeChoice { get; set; }

        public int PlayerId { get; set; }

        public PlayerResultsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            Title = "Results";
            State = ResultType.Active;
            RankingType = RankingType.Main;
            ShowRankingTypeChoice = false;
            ActiveResults = new ObservableCollectionRange<PlayerResult>();
            UnusedResults = new ObservableCollectionRange<PlayerResult>();
            PastResults = new ObservableCollectionRange<PlayerResult>();

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

                //TODO: it would be better to get a list of all categories a player is eligible for
                if (player.Gender == Gender.Female || (player.Age.HasValue && player.Age.Value < 18))
                {
                    ShowRankingTypeChoice = true;
                    OnPropertyChanged(nameof(ShowRankingTypeChoice));
                }

                ActiveResults.Clear();
                UnusedResults.Clear();
                PastResults.Clear();

                var activeResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.Active);
                if (activeResults.ResultsCount > 0)
                {
                    ActiveResults.AddRange(activeResults.Results);
                }
                
                var unusedResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.NonActive);
                if (unusedResults.ResultsCount > 0)
                {
                    UnusedResults.AddRange(unusedResults.Results);
                }
                
                var pastResults = await PinballRankingApiV2.GetPlayerResults(PlayerId, RankingType, ResultType.Inactive);
                if (pastResults.ResultsCount > 0)
                {
                    PastResults.AddRange(pastResults.Results);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}