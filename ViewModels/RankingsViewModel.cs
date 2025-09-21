using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR.Universal.Players.Search;
using PinballApi.Models.WPPR.Universal.Rankings;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    [QueryProperty(nameof(ShowSearch), "showSearch")]
    public partial class RankingsViewModel : BaseViewModel
    {
        public ObservableCollection<BaseRanking> Players { get; set; }
        public ObservableCollection<Country> Countries { get; set; }

        [ObservableProperty]
        private ObservableCollection<PlayerSearchResult> searchResults = new ObservableCollection<PlayerSearchResult>();

        [ObservableProperty]
        private BaseRanking selectedPlayer;

        [ObservableProperty]
        private PlayerSearchResult selectedSearchPlayer;

        [ObservableProperty]
        private Country countryToShow;

        [ObservableProperty]
        private bool isSearchMode = false;

        [ObservableProperty]
        private string searchText = string.Empty;

        private string showSearch = string.Empty;
        public string ShowSearch
        {
            get => showSearch;
            set
            {
                showSearch = value;
                if (value?.ToLower() == "true")
                {
                    IsSearchMode = true;
                    OnPropertyChanged(nameof(DisplayItems));
                    OnPropertyChanged(nameof(EmptyViewText));
                }
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // Only trigger search if we're in search mode
            if (IsSearchMode)
            {
                _ = Search();
            }
        }

        // Unified properties for single CollectionView
        public ObservableCollection<object> DisplayItems => IsSearchMode 
            ? new ObservableCollection<object>(SearchResults.Cast<object>()) 
            : new ObservableCollection<object>(Players.Cast<object>());

        public string EmptyViewText => IsSearchMode 
            ? Strings.PlayerSearchPage_NoPlayersFound 
            : Strings.RankingsPage_EmptyView;

        public object SelectedItem
        {
            get => IsSearchMode ? SelectedSearchPlayer : SelectedPlayer;
            set
            {
                if (IsSearchMode && value is PlayerSearchResult player)
                {
                    SelectedSearchPlayer = player;
                }
                else if (!IsSearchMode && value is BaseRanking ranking)
                {
                    SelectedPlayer = ranking;
                }
            }
        }

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
        private CancellationTokenSource searchCancellationTokenSource;

        public RankingsViewModel(IPinballRankingApi pinballRankingApi, ILogger<RankingsViewModel> logger) : base(logger)
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

                OnPropertyChanged(nameof(DisplayItems));
                OnPropertyChanged(nameof(EmptyViewText));
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
        public void ShowPlayerSearch()
        {
            IsSearchMode = true;
            OnPropertyChanged(nameof(DisplayItems));
            OnPropertyChanged(nameof(EmptyViewText));
        }

        [RelayCommand]
        public void CancelSearch()
        {
            IsSearchMode = false;
            SearchText = string.Empty;
            SearchResults.Clear();
            OnPropertyChanged(nameof(DisplayItems));
            OnPropertyChanged(nameof(EmptyViewText));
        }

        private async Task Search()
        {
            // Cancel any previous search
            searchCancellationTokenSource?.Cancel();
            searchCancellationTokenSource = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchResults.Clear();
                OnPropertyChanged(nameof(DisplayItems));
                return;
            }

            // Add a small delay to avoid too many API calls while typing
            try
            {
                await Task.Delay(300, searchCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return; // Search was cancelled
            }

            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                SearchResults.Clear();

                if (SearchText.Trim().Length > 0)
                {
                    var items = await PinballRankingApi.PlayerSearch(name: SearchText.Trim());

                    if (items.Results != null)
                    {
                        SearchResults = items.Results.ToObservableCollection();
                        OnPropertyChanged(nameof(DisplayItems));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching for player {text}", SearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ItemSelected()
        {
            if (IsSearchMode && SelectedSearchPlayer != null)
            {
                await Shell.Current.GoToAsync($"player-details?playerId={SelectedSearchPlayer.PlayerId}");
                SelectedSearchPlayer = null;
            }
            else if (!IsSearchMode && SelectedPlayer != null)
            {
                await Shell.Current.GoToAsync($"player-details?playerId={SelectedPlayer.PlayerId}");
                SelectedPlayer = null;
            }
        }
    }
}