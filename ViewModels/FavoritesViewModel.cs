using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using PinballApi.Models.WPPR.v2.Players;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class FavoritesViewModel : BaseViewModel
    {
        public ObservableCollection<Player> Players { get; set; }
        public bool IsPopulated => Players.Count > 0 || dataNotLoaded;

        private bool dataNotLoaded = true;

        public FavoritesViewModel(IConfiguration config) : base(config)
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
                            var tempList = await PinballRankingApiV2.GetPlayers(favorites.Select(n => n.PlayerID).ToList());

                            foreach (var player in tempList.OrderBy(i => i.PlayerStats.CurrentWpprRank))
                            {
                                Players.Add(player);
                            }
                        }
                        OnPropertyChanged("IsPopulated");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
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