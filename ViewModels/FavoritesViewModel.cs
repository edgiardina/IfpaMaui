using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using PinballApi.Models.WPPR.v2.Players;
using Microsoft.Extensions.Configuration;
using PinballApi;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public class FavoritesViewModel : BaseViewModel
    {
        public ObservableCollection<Player> Players { get; set; }
        public bool IsPopulated => Players.Count > 0 || dataNotLoaded;

        private bool dataNotLoaded = true;

        public FavoritesViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<FavoritesViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Favorites";
            Players = new ObservableCollection<Player>();
        }

        private Command _loadItemsCommand;
        public Command LoadItemsCommand
        {
            get
            {
                return _loadItemsCommand ?? (_loadItemsCommand = new Command<string>(async (text) =>
                {
                    if (IsBusy)
                        return;

                    IsBusy = true;

                    dataNotLoaded = false;

                    try
                    {
                        Players.Clear();

                        var favorites = await Settings.LocalDatabase.GetFavorites();
                        if (favorites.Any())
                        {
                            //Chunk into 50 to solve https://github.com/edgiardina/Ifpa/issues/148
                            foreach (var favoritesChunk in favorites.Chunk(50))
                            {
                                var tempList = await PinballRankingApiV2.GetPlayers(favoritesChunk.Select(n => n.PlayerID).ToList());

                                foreach (var player in tempList.OrderBy(i => i.PlayerStats.CurrentWpprRank))
                                {
                                    Players.Add(player);
                                }
                            }
                        }
                        OnPropertyChanged("IsPopulated");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading favorites");
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }));
            }
        }

    }
}