using System.Collections.ObjectModel;
using System.Text.Json;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v1.Players;
using static System.Net.Mime.MediaTypeNames;

namespace Ifpa.ViewModels
{
    public class PlayerSearchViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerSearchResult> Players { get; set; }

        public bool IsLoaded { get; set; } = false;

        public PlayerSearchViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerSearchViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            Title = "Player Search";
            Players = new ObservableCollection<PlayerSearchResult>();
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

                        if (text.Trim().Length > 2)
                        {
                            var items = await PinballRankingApi.SearchForPlayerByName(text.Trim());
                            foreach (var item in items.Search)
                            {
                                var serializedParent = JsonSerializer.Serialize(item);
                                var c = JsonSerializer.Deserialize<PlayerSearchResult>(serializedParent);

                                Players.Add(c);
                            }
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
            try
            {
                return await PinballRankingApi.SearchForPlayerByName(name.Trim());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching for player {player}", name);
                return new PlayerSearch(); 
            }
        }

    }
}