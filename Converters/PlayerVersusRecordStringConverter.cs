using PinballApi.Models.WPPR.Universal.Players;
using System.Globalization;

namespace Ifpa.Converters
{
    public class PlayerVersusRecordStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Transparent;

            var pvpRecord = (PlayerVersusRecord)value;

            //TODO: this sucks because we are getting dictionary items by convention not by name
            var colorDictionary = Application.Current.Resources.MergedDictionaries.First();

            return
                pvpRecord.WinCount > pvpRecord.LossCount ? colorDictionary["WinningRecord"]
                    : pvpRecord.WinCount < pvpRecord.LossCount ? colorDictionary["LosingRecord"]
                    : colorDictionary["TieRecord"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
