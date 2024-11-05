using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.Universal.Players;
using PlayerSearch = PinballApi.Models.WPPR.Universal.Players.Search.PlayerSearch;

namespace Ifpa.ViewModels
{
    public class PlayerSearchViewModel : BaseViewModel
    {
        public ObservableCollection<Player> Players { get; set; }

        public bool IsLoaded { get; set; } = false;

        private readonly PinballRankingApi rankingApi;

        public PlayerSearchViewModel(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<PlayerSearchViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Player Search";
            Players = new ObservableCollection<Player>();
            rankingApi = pinballRankingApi;
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

                        if (text.Trim().Length >= 4)
                        {
                            var items = await rankingApi.PlayerSearch(text.Trim());

                            Players = items.Results?.ToObservableCollection();
                            OnPropertyChanged("Players");
                        }
                        IsLoaded = true;
                        OnPropertyChanged("IsLoaded");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error searching for player {player}", text);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }));
            }
        }

        public async Task<PlayerSearch> SearchForPlayer(string name)
        {
            if(string.IsNullOrWhiteSpace(name) || name.Length <= 3)
                return new PlayerSearch();

            try
            {
                return await rankingApi.PlayerSearch(name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching for player {player}", name);
                return new PlayerSearch(); 
            }
        }

    }
}