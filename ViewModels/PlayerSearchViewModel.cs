using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using Newtonsoft.Json;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class PlayerSearchViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerSearchResult> Players { get; set; }

        public bool IsLoaded { get; set; } = false;

        public PlayerSearchViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
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

                        if (text.Trim().Length > 0)
                        {
                            var items = await PinballRankingApi.SearchForPlayerByName(text.Trim());
                            foreach (var item in items.Search)
                            {
                                var serializedParent = JsonConvert.SerializeObject(item);
                                var c = JsonConvert.DeserializeObject<PlayerSearchResult>(serializedParent);

                                Players.Add(c);
                            }
                        }
                        IsLoaded = true;
                        OnPropertyChanged("IsLoaded");
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