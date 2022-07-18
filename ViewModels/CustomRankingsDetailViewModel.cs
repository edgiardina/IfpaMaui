using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.ViewModels
{
    public class CustomRankingsDetailViewModel : BaseViewModel
    {
        public int ViewId { get; internal set; }
        public ObservableCollection<CustomRankingViewResult> ViewResults { get; set; }

        public ObservableCollection<Tournament> Tournaments { get; set; }

        public ObservableCollection<CustomRankingViewFilter> ViewFilters { get; set; }
        public bool IsPopulated => Tournaments.Count > 0 || dataNotLoaded;

        private bool dataNotLoaded = true;

        public CustomRankingsDetailViewModel(IConfiguration config) : base(config)
        {
            ViewResults = new ObservableCollection<CustomRankingViewResult>();
            Tournaments = new ObservableCollection<Tournament>();
            ViewFilters = new ObservableCollection<CustomRankingViewFilter>();

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
                        Tournaments.Clear();
                        ViewResults.Clear();
                        ViewFilters.Clear();

                        var tempList = await PinballRankingApiV2.GetRankingCustomView(ViewId);
                   
                        foreach (var tournament in tempList.Tournaments)
                        {
                            Tournaments.Add(tournament);
                        }

                        foreach (var viewResult in tempList.ViewResults)
                        {
                            ViewResults.Add(viewResult);
                        }

                        foreach (var viewFilters in tempList.ViewFilters)
                        {
                            ViewFilters.Add(viewFilters);
                        }

                        Title = tempList.Title;

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