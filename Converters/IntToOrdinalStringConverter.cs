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
            if(object.Equals(value, null) || object.Equals(value, 0))
            {
                return "Not Ranked";
            }

            // whether long or int, call value.OrdinalSuffix() extension method
            if(value is long longVal)
            {
                return longVal.OrdinalSuffix();
            }
            else if (value is int intVal)
            {
                return intVal.OrdinalSuffix();
            }

            throw new ArgumentException("Value must be of type long or int");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(((string)value).Substring(((string)value).Length - 3));
        }
    }
}
