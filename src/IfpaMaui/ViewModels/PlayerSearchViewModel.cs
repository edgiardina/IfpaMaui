using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR.Universal.Players.Search;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class PlayerSearchViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<PlayerSearchResult> players = new ObservableCollection<PlayerSearchResult>();

        [ObservableProperty]
        private bool isLoaded = false;

        [ObservableProperty]
        private PlayerSearchResult selectedPlayer;

        [ObservableProperty]
        private string text = string.Empty;

        private readonly IPinballRankingApi PinballRankingApi;

        public PlayerSearchViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerSearchViewModel> logger) : base(logger)
        {
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task Search()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Players.Clear();

                if (Text.Trim().Length > 0)
                {
                    var items = await PinballRankingApi.PlayerSearch(name: Text.Trim());

                    Players = items.Results?.ToObservableCollection();
                }
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching for player {text}", Text);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewPlayer()
        {
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedPlayer.PlayerId}");
            SelectedPlayer = null;
        }
    }
}