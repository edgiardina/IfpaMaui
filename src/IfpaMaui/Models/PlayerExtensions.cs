using PinballApi.Extensions;
using PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.Models
{
    public static class PlayerExtensions
    {

        public static string GetRank(this Player player)
        {
            return player.PlayerStats.Open?.CurrentRank.OrdinalSuffix() ?? Strings.NotRanked;
        }

        public static int GetIntegerRank(this Player player)
        {
            return (int)(player.PlayerStats.Open?.CurrentRank ?? 0);
        }
    }
}
