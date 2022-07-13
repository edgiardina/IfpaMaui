using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.Models
{
    /// <summary>
    /// this class is an unforunate hack to work around missing location data. 
    /// this way if we are missing a field like State or City we won't have extraneous spaces. 
    /// </summary>
    public class RankingWithFormattedLocation : RankingResult
    {
        public RankingWithFormattedLocation(RankingResult item)
        {
            BestFinish = item.BestFinish;
            EventCount = item.EventCount;
            EfficiencyPercent = item.EfficiencyPercent;
            RatingValue = item.RatingValue;
            LastMonthRank = item.LastMonthRank;
            CurrentWpprRank = item.CurrentWpprRank;
            WpprPoints = item.WpprPoints;
            City = item.City;
            StateProvince = item.StateProvince;
            CountryCode = item.CountryCode;
            CountryName = item.CountryName;
            Age = item.Age;
            LastName = item.LastName;
            FirstName = item.FirstName;
            PlayerId = item.PlayerId;
            BestFinishPosition = item.BestFinishPosition;
            BestTournamentId = item.BestTournamentId;
        }

        //Replace call at the end so that if a player is missing the 'state' we don't have an unsightly double space.
        public string Location => $"{City}{(!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(StateProvince) ? "," : string.Empty)} {StateProvince} {CountryName}".Trim().Replace("  ", " ");
    }
}
