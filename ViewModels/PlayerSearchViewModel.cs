using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Maui.Core.Extensions;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.ViewModels
{
    public class PlayerSearchViewModel : BaseViewModel
    {
        public ObservableCollection<Player> Players { get; set; }

        public bool IsLoaded { get; set; } = false;

        public PlayerSearchViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerSearchViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Player Search";
            Players = new ObservableCollection<Player>();
        }

        private Command _searchCommand;
        public Command SearchCommand
        {
            get
            {
                return _searchCommand ?? (_searchCommand = new Command<string>(async (text) =>
                {
                    if (IsBusy)
                        return;

                    IsBusy = true;

                    try
                    {
                        Players.Clear();

                        if (text.Trim().Length > 0)
                        {
                            var items = await PinballRankingApiV2.GetPlayersBySearch(new PlayerSearchFilter() { Name = text.Trim() });

                            Players = items.Results.ToObservableCollection();
                            OnPropertyChanged("Players");
                        }
                        IsLoaded = true;
                        OnPropertyChanged("IsLoaded");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error searching for player", text);
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