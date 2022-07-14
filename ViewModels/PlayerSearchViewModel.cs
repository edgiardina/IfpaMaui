using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui;
using PinballApi.Models.WPPR.v1.Players;

namespace Ifpa.ViewModels
{
    public class PlayerSearchViewModel : BaseViewModel
    {
        public ObservableCollection<Search> Players { get; set; }

        public bool IsPopulated => Players != null && Players.Count > 0;

        public PlayerSearchViewModel(IConfiguration config) : base(config)
        {
            Title = "Player Search";
            Players = new ObservableCollection<Search>();
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
                                Players.Add(item);
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