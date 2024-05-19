using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Extensions.Logging;
using PinballApi;
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

        public CustomRankingsDetailViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<CustomRankingsDetailViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
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

                        Tournaments = tempList.Tournaments.ToObservableCollection();
                        OnPropertyChanged(nameof(Tournaments));

                        ViewResults = tempList.ViewResults.ToObservableCollection();
                        OnPropertyChanged(nameof(ViewResults));


                        foreach (var viewFilter in tempList.ViewFilters)
                        {
                            //For some reason some of these have carriage return / line feed. so strip that out.
                            viewFilter.Name = viewFilter.Name.Trim();

                            ViewFilters.Add(viewFilter);
                        }

                        Title = tempList.Title;

                        OnPropertyChanged("IsPopulated");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading custom rankings detail for view {0}", ViewId);
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