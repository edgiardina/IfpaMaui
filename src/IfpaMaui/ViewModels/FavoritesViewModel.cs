using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class FavoritesViewModel : BaseViewModel
    {
        // Observable so a swipe-to-delete removes the row in place instead of needing a reload
        [ObservableProperty]
        private ObservableCollection<Player> players = new ObservableCollection<Player>();

        [ObservableProperty]
        private bool isPopulated;

        private bool dataNotLoaded = true;

        private readonly IPinballRankingApi PinballRankingApi;

        public FavoritesViewModel(IPinballRankingApi pinballRankingApi, ILogger<FavoritesViewModel> logger) : base(logger)
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
                var tempList = new List<Player>();
                var favorites = await Settings.LocalDatabase.GetFavorites();
                if (favorites.Any())
                {
                    //Chunk into 50 to solve https://github.com/edgiardina/Ifpa/issues/148
                    foreach (var favoritesChunk in favorites.Chunk(50))
                    {
                        tempList.AddRange(await PinballRankingApi.GetPlayers(favoritesChunk.Select(n => n.PlayerID).ToList()));                        
                    }

                    Players = new ObservableCollection<Player>(tempList.OrderBy(i => i.PlayerStats.Open.CurrentRank));
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
        public async Task DeletePlayer(Player player)
        {
            try
            {
                await Settings.LocalDatabase.RemoveFavorite((int)player.PlayerId);

                Players.Remove(player);
                IsPopulated = Players.Count > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting favorite");
            }
        }

        [RelayCommand]
        public async Task SelectPlayer(long playerId)
        {
            await Shell.Current.GoToAndPopExistingAsync($"player-details?playerId={playerId}");

        }
    }
}