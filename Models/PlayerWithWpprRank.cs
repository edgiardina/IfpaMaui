using PinballApi.Models.WPPR.v1.Players;

namespace Ifpa.Models
{
    public class PlayerWithWpprRank : Player
    {
        public PlayerWithWpprRank(Player player, int wpprRank)
        {
            WpprRank = wpprRank;
            City = player.City;
            State = player.State;
            CountryCode = player.CountryCode;
            CountryName = player.CountryName;
            Age = player.Age;
            LastName = player.LastName;
            FirstName = player.FirstName;
            PlayerId = player.PlayerId;
            Age = player.Age;
            ExcludedFlag = player.ExcludedFlag;
            IfpaRegistered = player.IfpaRegistered;
        }

        public int WpprRank { get; set; }
    }
}
