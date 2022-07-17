using Microsoft.Extensions.Configuration;
using PinballApi.Extensions;
using PinballApi.Models.v2.WPPR;
using PinballApi.Models.WPPR.v2.Players;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Ifpa.ViewModels
{
    public class PlayerDetailViewModel : BaseViewModel
    {
        public Command LoadItemsCommand { get; set; }
        public Command PostPlayerLoadCommand { get; set; }

        public int PlayerId { get; set; }
        public int LastTournamentCount { get; set; }
        private Player playerRecord = new Player { PlayerStats = new PinballApi.Models.WPPR.v1.Players.PlayerStats { }, ChampionshipSeries = new List<ChampionshipSeries> { } };

        public Player PlayerRecord
        {
            get { return playerRecord; }
            set
            {
                playerRecord = value;
                OnPropertyChanged(null);
            }
        }
        public ObservableCollection<RankHistory> PlayerRankHistory { get; set; }

        public ObservableCollection<RatingHistory> PlayerRatingHistory { get; set; }

        public string Name => PlayerRecord.FirstName + " " + PlayerRecord.LastName;

        public string Initials => PlayerRecord.Initials;

        public string Rank => PlayerRecord.PlayerStats.CurrentWpprRank.OrdinalSuffix();

        public string Rating => PlayerRecord.PlayerStats.RatingsRank.HasValue ? PlayerRecord.PlayerStats.RatingsRank.Value.OrdinalSuffix() : "Not Ranked";

        public double? RatingValue => PlayerRecord.PlayerStats.RatingsValue;

        public string EffPercent => PlayerRecord.PlayerStats.EfficiencyRank.HasValue ? PlayerRecord.PlayerStats.EfficiencyRank.Value.OrdinalSuffix() : "Not Ranked";

        public double? EfficiencyValue => PlayerRecord.PlayerStats.EfficiencyValue;

        public double CurrentWpprValue => PlayerRecord.PlayerStats.CurrentWpprValue;

        public string LastMonthRank => PlayerRecord.PlayerStats.LastMonthRank.OrdinalSuffix();

        public string LastYearRank => PlayerRecord.PlayerStats.LastYearRank.OrdinalSuffix();

        public string HighestRank => PlayerRecord.PlayerStats.HighestRank.OrdinalSuffix();

        public DateTime? HighestRankDate => PlayerRecord.PlayerStats.HighestRankDate;

        public double TotalWpprs => PlayerRecord.PlayerStats.WpprPointsAllTime;

        public string BestFinish => PlayerRecord.PlayerStats.BestFinish.OrdinalSuffix();

        public int BestFinishCount => PlayerRecord.PlayerStats.BestFinishCount;

        public int AvgFinish => PlayerRecord.PlayerStats.AverageFinish;

        public int AvgFinishLastYear => PlayerRecord.PlayerStats.AverageFinishLastYear;

        public int TotalEvents => PlayerRecord.PlayerStats.TotalEventsAllTime;

        public int TotalActiveEvents => PlayerRecord.PlayerStats.TotalActiveEvents;
        
        public int EventsOutsideCountry => PlayerRecord.PlayerStats.TotalEventsAway;

        public string PlayerAvatar
        {
            get
            {
                if (PlayerRecord.ProfilePhoto != null)
                    return PlayerRecord.ProfilePhoto?.ToString();
                else
                    return AppSettings.PlayerProfileNoPicUrl;
            }
        }

        public bool? HasChampionshipSeriesData => PlayerRecord.ChampionshipSeries?.Any();

        public string CountryFlag => $"https://flagcdn.com/w80/{PlayerRecord.CountryCode?.ToLower()}.png";

        //Replace call at the end so that if a player is missing the 'state' we don't have an unsightly double space.
        public string Location => $"{PlayerRecord.City}{(!string.IsNullOrEmpty(PlayerRecord.City) && !string.IsNullOrEmpty(PlayerRecord.StateProvince) ? "," : string.Empty)} {PlayerRecord.StateProvince} {PlayerRecord.CountryName}".Trim().Replace("  ", " ");

        public bool IsRegistered => PlayerRecord.IfpaRegistered;
        
        public PlayerDetailViewModel(IConfiguration config) : base(config)
        {
            PlayerRankHistory = new ObservableCollection<RankHistory>();
            PlayerRatingHistory = new ObservableCollection<RatingHistory>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            try
            {
                if (PlayerId > 0)
                {
                    IsBusy = true;
                    var playerData = await PinballRankingApiV2.GetPlayer(PlayerId);
                    var playerHistoryData = await PinballRankingApiV2.GetPlayerHistory(PlayerId);

                    if(playerHistoryData.RankHistory != null)
                        PlayerRankHistory = new ObservableCollection<RankHistory>(playerHistoryData.RankHistory);

                    if (playerHistoryData.RatingHistory != null)
                        PlayerRatingHistory = new ObservableCollection<RatingHistory>(playerHistoryData.RatingHistory);

                    PlayerRecord = playerData;
                    
                    //TODO: restore app links
                    //AddPlayerToAppLinks();

                    if (PostPlayerLoadCommand != null)
                    {
                        PostPlayerLoadCommand.Execute(null);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddPlayerToAppLinks()
        {
            var url = $"https://www.ifpapinball.com/player.php?p={PlayerId}";

            var entry = new AppLinkEntry
            {
                Title = Name,
                Description = Rank,
                AppLinkUri = new Uri(url, UriKind.RelativeOrAbsolute),
                IsLinkActive = true,
                Thumbnail = ImageSource.FromUri(new Uri(PlayerAvatar, UriKind.RelativeOrAbsolute))
            };

            entry.KeyValues.Add("contentType", "Player");
            entry.KeyValues.Add("appName", "IFPA Companion");

            Application.Current.AppLinks.RegisterLink(entry);
        }

    }
}
