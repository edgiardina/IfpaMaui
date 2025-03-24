using PinballApi.Extensions;
using PinballApi.Models.WPPR.Universal.Players;
using System.Globalization;

namespace Ifpa.Converters
{
    public class PlayerToRankConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is PlayerStats player)
            {
                var firstSeriesRank = player.System.FirstOrDefault()?.CurrentRank;

                if (firstSeriesRank.HasValue == false)
                {
                    return Strings.NotRanked;
                }

                return firstSeriesRank.Value.OrdinalSuffix();
            }

            throw new ArgumentException("Value is not a Player object");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(((string)value).Substring(((string)value).Length - 3));
        }
    }
}
