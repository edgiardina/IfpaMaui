using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Rankings;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class RankingsViewModel : BaseViewModel
    {
        public ObservableCollection<BaseRanking> Players { get; set; }
        public ObservableCollection<Country> Countries { get; set; }

        [ObservableProperty]
        private BaseRanking selectedPlayer;

        [ObservableProperty]
        private Country countryToShow;

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

        [ObservableProperty]
        private RankingType currentRankingType;

        [ObservableProperty]
        private RankingSystem currentRankingSystem;

        [ObservableProperty]
        private TournamentType currentProRankingType;

        public List<TournamentType> ProRankingTypes => new List<TournamentType> { TournamentType.Open, TournamentType.Women };
        public List<RankingType> RankingTypes => new List<RankingType> { RankingType.Pro, RankingType.Wppr, RankingType.Women, RankingType.Youth, RankingType.Country };

        public List<RankingSystem> RankingSystems => new List<RankingSystem> { RankingSystem.Open, RankingSystem.Restricted };

        public readonly Country DefaultCountry = new Country { CountryName = "United States", CountryCode = "US" };

        private readonly IPinballRankingApi PinballRankingApi;

        public RankingsViewModel(PinballRankingApiV2 pinballRankingApiV2, IPinballRankingApi pinballRankingApi, ILogger<RankingsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            CountOfItemsToFetch = 100;
            StartingPosition = 1;
            Players = new ObservableCollection<BaseRanking>();
            Countries = new ObservableCollection<Country>();

            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
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

        [RelayCommand]
        public async Task ShowRankingFilter()
        {
            await Shell.Current.GoToAsync("rankings-filter");
        }

        [RelayCommand]
        public async Task ShowPlayerSearch()
        {
            await Shell.Current.GoToAsync("player-search");
        }

        [RelayCommand]
        public async Task ShowPlayerDetail()
        {
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedPlayer.PlayerId}");
            SelectedPlayer = null;
        }
    }
}