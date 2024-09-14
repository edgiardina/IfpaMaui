using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v2.Directors;
using PinballApi;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public class DirectorsViewModel : BaseViewModel
    {
        public ObservableCollection<Director> NacsDirectors { get; set; }
        public ObservableCollection<Director> CountryDirectors { get; set; }

        public DirectorsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<DirectorsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Directors";
            NacsDirectors = new ObservableCollection<Director>();
            CountryDirectors = new ObservableCollection<Director>();
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
                    
                    try
                    {
                        NacsDirectors.Clear();
                        CountryDirectors.Clear();
                      
                        var nacsDirectors = await PinballRankingApiV2.GetNacsDirectors();
                        var countryDirectors = await PinballRankingApiV2.GetCountryDirectors();

                        foreach (var director in nacsDirectors)
                        {
                            NacsDirectors.Add(director);
                        }

                        foreach (var director in countryDirectors)
                        {
                            CountryDirectors.Add(director);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading directors");
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