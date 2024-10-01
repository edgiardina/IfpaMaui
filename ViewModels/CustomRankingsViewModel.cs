using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v2.Rankings;
using Microsoft.Extensions.Configuration;
using PinballApi;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public class CustomRankingsViewModel : BaseViewModel
    {
        public ObservableCollection<CustomRankingView> CustomRankings { get; set; }
        public bool IsPopulated => CustomRankings.Count > 0 || dataNotLoaded;

        private bool dataNotLoaded = true;

        public CustomRankingsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<CustomRankingsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Custom Rankings";
            CustomRankings = new ObservableCollection<CustomRankingView>();
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
                        CustomRankings.Clear();                 

                        var tempList = await PinballRankingApiV2.GetRankingCustomViewList();
                   
                        foreach (var customRankingView in tempList.CustomRankingView)
                        {
                            CustomRankings.Add(customRankingView);
                        }

                        OnPropertyChanged("IsPopulated");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading custom rankings");
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