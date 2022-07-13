using PinballApi.Extensions;
using System;
using System.Globalization;
using Microsoft.Maui;

namespace Ifpa.Converters
{
    public class IntToOrdinalStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(object.Equals(value, null))
            {
                return "Not Ranked";
            }

            return ((int)value).OrdinalSuffix();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(((string)value).Substring(((string)value).Length - 3));
        }
    }
}
