using PinballApi.Extensions;
using System.Globalization;

namespace Ifpa.Converters
{
    public class LongToOrdinalStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(object.Equals(value, null) || object.Equals(value, 0))
            {
                return "Not Ranked";
            }

            return ((long)value).OrdinalSuffix();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return long.Parse(((string)value).Substring(((string)value).Length - 3));
        }
    }
}
