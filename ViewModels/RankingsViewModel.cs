using System.Collections.ObjectModel;
using System.Windows.Input;
using PinballApi;
using Microsoft.Extensions.Logging;
using PinballApi.Models.WPPR.Universal.Rankings;
using PinballApi.Models.WPPR.Universal;
using Ifpa.Models;

namespace Ifpa.ViewModels
{
    public class RankingsViewModel : BaseViewModel
    {
        public ObservableCollection<BaseRanking> Players { get; set; }
        public ObservableCollection<Country> Countries { get; set; }

        public Country CountryToShow { get; set; }

        public ICommand LoadItemsCommand { get; set; }

        private int startingPosition;
        public int StartingPosition
        {
            get { return startingPosition; }
            set { SetProperty(ref startingPosition, value); }
        }

        private int countOfItemsToFetch;
        public int CountOfItemsToFetch
        {
            get { return countOfItemsToFetch; }
            set { SetProperty(ref countOfItemsToFetch, value); }
        }

        public RankingType CurrentRankingType { get; set; }
        public RankingSystem CurrentRankingSystem { get; set; }

        public TournamentType CurrentProRankingType { get; set; }

        public List<string> ProRankingTypes => Enum.GetNames(typeof(TournamentType))
                                                   .Except(new List<string> { TournamentType.Main.ToString(), TournamentType.Youth.ToString() })
                                                   .ToList();
        public List<string> RankingTypes => Enum.GetNames(typeof(RankingType)).ToList();

        public List<string> RankingSystems => Enum.GetNames(typeof(RankingSystem)).ToList();

        public readonly Country DefaultCountry = new Country { CountryName = "United States", CountryCode = "US" };

        private readonly PinballRankingApi PinballRankingApi;

        public RankingsViewModel(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<RankingsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            CountOfItemsToFetch = 100;
            StartingPosition = 1;
            Players = new ObservableCollection<BaseRanking>();
            Countries = new ObservableCollection<Country>();
            LoadItemsCommand = new Command(
                execute: () => ExecuteLoadItemsCommand(),
                canExecute: () =>
                {
                    return !IsBusy;
                });

            PinballRankingApi = pinballRankingApi;
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                if (Countries.Count == 0)
                {
                    var countries = await PinballRankingApi.GetRankingCountries();

                    foreach (var stat in countries.Country.OrderBy(n => n.CountryName))
                    {
                        Countries.Add(stat);
                    }
                }

                if (CountryToShow == null)
                {
                    CountryToShow = DefaultCountry;
                }

                CountryToShow = Countries.Single(n => n.CountryName == CountryToShow.CountryName);
                OnPropertyChanged(nameof(CountryToShow));

                Players.Clear();

                if (CurrentRankingType == RankingType.Pro)
                {
                    var proItems = await PinballRankingApi.ProRankingSearch(CurrentProRankingType);
                    if (proItems.Rankings != null)
                    {
                        foreach (var item in proItems.Rankings)
                        {
                            Players.Add(item);
                        }
                    }
                }
                else
                {
                    var items = await PinballRankingApi.RankingSearch(CurrentRankingType, CurrentRankingSystem, CountOfItemsToFetch, StartingPosition, countryCode: CountryToShow.CountryCode);
                    if (items.Rankings != null)
                    {
                        foreach (var item in items.Rankings)
                        {
                            Players.Add(item);
                        }
                    }
                }

                OnPropertyChanged(nameof(CurrentProRankingType));
                OnPropertyChanged(nameof(CurrentRankingType));
                OnPropertyChanged(nameof(CurrentRankingSystem));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading rankings for {0}", CurrentRankingType);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}