using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.ViewModels
{
    public partial class FavoritesViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<Player> players = new List<Player>();

        [ObservableProperty]
        private bool isPopulated;

        private bool dataNotLoaded = true;

        public FavoritesViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<FavoritesViewModel> logger) : base(pinballRankingApiV2, logger)
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
                var tempList = new List<Player>();
                var favorites = await Settings.LocalDatabase.GetFavorites();
                if (favorites.Any())
                {
                    //Chunk into 50 to solve https://github.com/edgiardina/Ifpa/issues/148
                    foreach (var favoritesChunk in favorites.Chunk(50))
                    {
                        tempList.AddRange(await PinballRankingApiV2.GetPlayers(favoritesChunk.Select(n => n.PlayerID).ToList()));                        
                    }

                    Players = tempList.OrderBy(i => i.PlayerStats.CurrentWpprRank).ToList();
                }

                IsPopulated = Players.Count > 0 || dataNotLoaded;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading favorites");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeletePlayer(int playerId)
        {
            try
            {
                await Settings.LocalDatabase.RemoveFavorite(playerId);

                Players.Remove(Players.Single(n => n.PlayerId == playerId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting favorite");
            }
        }

        [RelayCommand]
        public async Task SelectPlayer(int playerId)
        {
            await Shell.Current.GoToAsync($"player-details?playerId={playerId}");

        }
    }
}