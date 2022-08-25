using PinballApi.Extensions;
using System;
using System.Globalization;
using Microsoft.Maui;
using System.Web;

namespace Ifpa.Converters
{
    public class HtmlDecodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            return HttpUtility.HtmlDecode((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(((string)value).Substring(((string)value).Length - 3));
        }
    }
}
