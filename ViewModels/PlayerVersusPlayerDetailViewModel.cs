using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class PlayerVersusPlayerDetailViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<PlayerVersusPlayerComparisonRecord> playerVersusPlayer = new ObservableCollection<PlayerVersusPlayerComparisonRecord>();

        [ObservableProperty]
        private string playerOneInitials;

        [ObservableProperty]
        private string playerTwoInitials;

        [ObservableProperty]
        private PlayerVersusPlayerComparisonRecord selectedPlayerVersusPlayer;

        public int PlayerOneId { get; set; }
        public int PlayerTwoId { get; set; }
        public PlayerVersusPlayerDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerVersusPlayerDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                PlayerVersusPlayer.Clear();
                var pvpResults = await PinballRankingApiV2.GetPlayerVersusPlayerComparison(PlayerOneId, PlayerTwoId);

                PlayerVersusPlayer = pvpResults.ComparisonRecords.OrderByDescending(n => n.EventDate).ToObservableCollection();

                Title = $"{pvpResults.PlayerOne.FirstName} {pvpResults.PlayerOne.LastName} vs {pvpResults.PlayerTwo.FirstName} {pvpResults.PlayerTwo.LastName}";

                PlayerOneInitials = pvpResults.PlayerOne.FirstName.FirstOrDefault().ToString() + pvpResults.PlayerOne.LastName.FirstOrDefault().ToString();
                PlayerTwoInitials = pvpResults.PlayerTwo.FirstName.FirstOrDefault().ToString() + pvpResults.PlayerTwo.LastName.FirstOrDefault().ToString();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error loading player versus player for players {0} {1}", PlayerOneId, PlayerTwoId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewTournamentResults()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedPlayerVersusPlayer.TournamentId}");
            SelectedPlayerVersusPlayer = null;
        }
    }
}