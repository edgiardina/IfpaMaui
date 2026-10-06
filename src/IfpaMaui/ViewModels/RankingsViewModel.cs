using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Rankings;
using System.Collections.ObjectModel;
using Ifpa.Models;

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

        partial void OnCountryToShowChanged(Country value) => OnPropertyChanged(nameof(SelectedCountryName));

        private int startingPosition;
        public int StartingPosition
        {
            get { return startingPosition; }
            set
            {
                if (SetProperty(ref startingPosition, value))
                    OnFilterChanged();
            }
        }

        private int countOfItemsToFetch;
        public int CountOfItemsToFetch
        {
            get { return countOfItemsToFetch; }
            set
            {
                if (SetProperty(ref countOfItemsToFetch, value))
                    OnFilterChanged();
            }
        }

        [ObservableProperty]
        private RankingType currentRankingType;

        partial void OnCurrentRankingTypeChanged(RankingType value) => OnFilterChanged();

        [ObservableProperty]
        private RankingSystem currentRankingSystem;

        partial void OnCurrentRankingSystemChanged(RankingSystem value) => OnFilterChanged();

        [ObservableProperty]
        private TournamentType currentProRankingType;

        partial void OnCurrentProRankingTypeChanged(TournamentType value) => OnFilterChanged();

        // The filter menu on the Rankings page binds to the members below

        public bool IsWomenRanking => CurrentRankingType == RankingType.Women;

        public bool IsProRanking => CurrentRankingType == RankingType.Pro;

        public bool IsCountryRanking => CurrentRankingType == RankingType.Country && Countries.Count > 0;

        // The pro rankings are one fixed list
        public bool HasPaging => CurrentRankingType != RankingType.Pro;

        public string PlayerCountTitle => $"{Strings.RankingsFilterModalPage_Players}: {CountOfItemsToFetch}";

        public string StartingRankTitle => $"{Strings.RankingsFilterModalPage_StartingRank}: {StartingPosition}";

        // The default pro ranking type is Main, which is not a choice in the menu. The menu shows Open for it.
        public TournamentType SelectedProRankingType
        {
            get => ProRankingTypes.Contains(CurrentProRankingType) ? CurrentProRankingType : TournamentType.Open;
            set
            {
                // The binding can write the shown value back. Do not change Main to Open because of that.
                if (value != SelectedProRankingType)
                    CurrentProRankingType = value;
            }
        }

        public string SelectedCountryName
        {
            get => CountryToShow?.CountryName;
            set
            {
                var country = Countries.FirstOrDefault(n => n.CountryName == value);
                if (country == null || country.CountryName == CountryToShow?.CountryName)
                    return;

                CountryToShow = country;
                OnFilterChanged();
            }
        }

        // False until RestoreFilter reads the saved filter. Before that, a change does not save or load.
        private bool isFilterRestored;
        private bool isReloading;
        private bool isReloadPending;

        public List<TournamentType> ProRankingTypes => new List<TournamentType> { TournamentType.Open, TournamentType.Women };
        public List<RankingType> RankingTypes => new List<RankingType> { RankingType.Pro, RankingType.Wppr, RankingType.Women, RankingType.Youth, RankingType.Country };

        public List<RankingSystem> RankingSystems => new List<RankingSystem> { RankingSystem.Open, RankingSystem.Restricted };

        public readonly Country DefaultCountry = new Country { CountryName = "United States", CountryCode = "US" };

        private readonly IPinballRankingApi PinballRankingApi;

        public RankingsViewModel(IPinballRankingApi pinballRankingApi, ILogger<RankingsViewModel> logger) : base(logger)
        {
            // Set the fields, because the properties save the filter and load the list
            countOfItemsToFetch = 100;
            startingPosition = 1;
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

        /// <summary>
        /// Reads the saved filter. This does not load the list.
        /// </summary>
        public void RestoreFilter()
        {
            isFilterRestored = false;
            try
            {
                CountOfItemsToFetch = Preferences.Get("PlayerCount", CountOfItemsToFetch);
                StartingPosition = Preferences.Get("StartingRank", StartingPosition);

                // if ranking type preference is 'Main', reset to 'Wppr' type
                if (Preferences.Get("RankingType", CurrentRankingType.ToString()) == "Main")
                {
                    Preferences.Set("RankingType", "Wppr");
                }

                CurrentRankingType = Enum.Parse<RankingType>(Preferences.Get("RankingType", CurrentRankingType.ToString()));
                CurrentRankingSystem = Enum.Parse<RankingSystem>(Preferences.Get("RankingSystem", CurrentRankingSystem.ToString()));
                CurrentProRankingType = Enum.Parse<TournamentType>(Preferences.Get("ProRankingType", CurrentProRankingType.ToString()));

                CountryToShow = new Country { CountryName = Preferences.Get("CountryName", DefaultCountry.CountryName) };
            }
            finally
            {
                isFilterRestored = true;
            }
        }

        // Saves the filter and loads the list again
        private void OnFilterChanged()
        {
            OnPropertyChanged(nameof(IsWomenRanking));
            OnPropertyChanged(nameof(IsProRanking));
            OnPropertyChanged(nameof(IsCountryRanking));
            OnPropertyChanged(nameof(HasPaging));
            OnPropertyChanged(nameof(PlayerCountTitle));
            OnPropertyChanged(nameof(StartingRankTitle));
            OnPropertyChanged(nameof(SelectedProRankingType));

            if (!isFilterRestored)
                return;

            Preferences.Set("PlayerCount", CountOfItemsToFetch);
            Preferences.Set("StartingRank", StartingPosition);
            Preferences.Set("RankingType", CurrentRankingType.ToString());
            Preferences.Set("RankingSystem", CurrentRankingSystem.ToString());
            Preferences.Set("ProRankingType", CurrentProRankingType.ToString());
            if (CountryToShow != null)
            {
                Preferences.Set("CountryName", CountryToShow.CountryName);
            }

            _ = Reload();
        }

        /// <summary>
        /// Loads the list. A filter change during a load starts one more load, so the list always shows the last filter.
        /// </summary>
        public async Task Reload()
        {
            isReloadPending = true;
            if (isReloading)
                return;

            isReloading = true;
            try
            {
                while (isReloadPending)
                {
                    isReloadPending = false;
                    await LoadItems();
                }
            }
            finally
            {
                isReloading = false;
            }

            // The first load gets the country list
            OnPropertyChanged(nameof(IsCountryRanking));
        }

        [RelayCommand]
        public async Task PromptForStartingRank()
        {
            var answer = await Shell.Current.DisplayPromptAsync(
                Strings.RankingsFilterModalPage_StartingRank,
                null,
                Strings.OK,
                Strings.Cancel,
                keyboard: Keyboard.Numeric,
                initialValue: StartingPosition.ToString());

            if (int.TryParse(answer, out var startingRank) && startingRank >= 1)
            {
                StartingPosition = startingRank;
            }
        }

        [RelayCommand]
        public async Task ShowPlayerSearch()
        {
            await Shell.Current.GoToAsync("player-search");
        }

        [RelayCommand]
        public async Task ShowPlayerDetail()
        {
            await Shell.Current.GoToAndPopExistingAsync($"player-details?playerId={SelectedPlayer.PlayerId}");
            SelectedPlayer = null;
        }
    }
}