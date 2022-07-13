using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using PinballApi.Models.WPPR.v2.Rankings;
using PinballApi.Models.WPPR.v2;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class RankingsViewModel : BaseViewModel
    {
        public ObservableCollection<RankingWithFormattedLocation> Players { get; set; }
        public ObservableCollection<Country> Countries { get; set; }

        public Country CountryToShow { get; set; } 

        public Command LoadItemsCommand { get; set; }

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

        private bool showOverallRank;

        public bool ShowOverallRank
        {
            get { return showOverallRank; }
            set { SetProperty(ref showOverallRank, value); }
        }

        public RankingType CurrentRankingType { get; set; }
        public List<string> RankingTypes
        {
            get
            {
                var types = Enum.GetNames(typeof(RankingType)).ToList();
                types.Remove("WPPR");
                types.Remove("Elite");
                return types;
            }
        }

        public List<string> TournamentTypes => Enum.GetNames(typeof(TournamentType)).ToList();   
        
        public TournamentType CurrentTournamentType { get; set; }

        public readonly Country DefaultCountry = new Country { CountryName = "United States" };        

        public RankingsViewModel(IConfiguration config) : base(config)
        {
            Title = "Rankings";
            CountOfItemsToFetch = 100;
            StartingPosition = 1;
            Players = new ObservableCollection<RankingWithFormattedLocation>();
            Countries = new ObservableCollection<Country>();           
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
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
                    var countries = await PinballRankingApiV2.GetRankingCountries();
                    foreach (var stat in countries.Country.OrderBy(n => n.CountryName))
                    {
                        Countries.Add(stat);
                    }

                    if(CountryToShow == null)
                    {
                        CountryToShow = DefaultCountry;
                    }

                    CountryToShow = Countries.Single(n => n.CountryName == CountryToShow.CountryName);
                    OnPropertyChanged(nameof(CountryToShow));
                }

                Players.Clear();

                if (CurrentRankingType == RankingType.Main)
                {
                    ShowOverallRank = false;

                    var items = await PinballRankingApiV2.GetWpprRanking(StartingPosition, CountOfItemsToFetch);
                    foreach (var item in items.Rankings)
                    {
                        Players.Add(new RankingWithFormattedLocation(item));
                    }
                }
                else if(CurrentRankingType == RankingType.Women)
                {
                    ShowOverallRank = CurrentTournamentType == TournamentType.Open;

                    var items = await PinballRankingApiV2.GetRankingForWomen(CurrentTournamentType, StartingPosition, CountOfItemsToFetch);
                    foreach (var item in items.Rankings)
                    {
                        Players.Add(new RankingWithFormattedLocation(item));
                    }
                }
                else if (CurrentRankingType == RankingType.Youth)
                {
                    ShowOverallRank = true;

                    var items = await PinballRankingApiV2.GetRankingForYouth(StartingPosition, CountOfItemsToFetch);
                    foreach (var item in items.Rankings)
                    {
                        Players.Add(new RankingWithFormattedLocation(item));
                    }
                }
                else if (CurrentRankingType == RankingType.Country)
                {
                    ShowOverallRank = true;

                    var items = await PinballRankingApiV2.GetRankingForCountry(CountryToShow.CountryName, StartingPosition, CountOfItemsToFetch);
                    foreach (var item in items.Rankings)
                    {
                        Players.Add(new RankingWithFormattedLocation(item));
                    }
                }
                //else if (CurrentRankingType == RankingType.Elite)
                //{
                //    var items = await PinballRankingApiV2.GetEliteRanking(StartingPosition, CountOfItemsToFetch);
                //    foreach (var item in items.Rankings)
                //    {
                //        Players.Add(new RankingWithFormattedLocation(item));
                //    }
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}