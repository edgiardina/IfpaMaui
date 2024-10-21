using TournamentSearch = PinballApi.Models.WPPR.Universal.Tournaments.Search.Tournament;

namespace Ifpa.Models
{
    public class TournamentWithDistance : TournamentSearch
    {
        public TournamentWithDistance(TournamentSearch tournament, long distance)
        {
            this.TournamentId = tournament.TournamentId;
            this.TournamentName = tournament.TournamentName;
            this.EventName = tournament.EventName;
            this.EventType = tournament.EventType;
            this.Address1 = tournament.Address1;
            this.Address2 = tournament.Address2;
            this.City = tournament.City;
            this.Stateprov = tournament.Stateprov;
            this.PostalCode = tournament.PostalCode;
            this.CountryCode = tournament.CountryCode;
            this.CountryName = tournament.CountryName;
            this.EventStartDate = tournament.EventStartDate;
            this.EventEndDate = tournament.EventEndDate;
            this.Latitude = tournament.Latitude;
            this.Longitude = tournament.Longitude;
            this.RawAddress = tournament.RawAddress;
            this.PrivateFlag = tournament.PrivateFlag;
            this.RankingSystem = tournament.RankingSystem;
            this.Winner = tournament.Winner;
            this.Website = tournament.Website;
            this.CertifiedFlag = tournament.CertifiedFlag;
            this.DirectorId = tournament.DirectorId;
            this.DirectorName = tournament.DirectorName;
            this.FinalsFormat = tournament.FinalsFormat;
            this.PlayerCount = tournament.PlayerCount;
            this.PreregistrationDate = tournament.PreregistrationDate;
            this.QualifyingFormat = tournament.QualifyingFormat;
            this.Distance = distance;
        }

        public long Distance { get; set; }
    }
}
