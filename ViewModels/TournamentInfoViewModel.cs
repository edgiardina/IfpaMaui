using Maui.BottomSheet.SheetBuilder;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.ViewModels
{
    [QueryProperty(nameof(TournamentId), "tournamentId")]
    public class TournamentInfoViewModel : BaseViewModel, IQueryAttributable
    {
        public Command LoadItemsCommand { get; set; }
        public int TournamentId { get; set; }

        public Tournament TournamentDetails { get; set; }

        public TournamentInfoViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<TournamentResultsViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        private async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                TournamentDetails = await PinballRankingApiV2.GetTournament(TournamentId);

                OnPropertyChanged(nameof(TournamentDetails));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading tournament results id {0}", TournamentId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if(query.ContainsKey("tournamentId"))
            {
                TournamentId = Convert.ToInt32(query["tournamentId"]);
                ExecuteLoadItemsCommand();
            }
        }
    }
}
