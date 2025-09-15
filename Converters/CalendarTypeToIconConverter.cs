using System.Globalization;
using Ifpa.ViewModels;

namespace Ifpa.Converters
{
    public class CalendarTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CalendarType calendarType)
            {
                return calendarType == CalendarType.MapAndList
                    ? new FontImageSource 
                    { 
                        Glyph = (string)Application.Current.Resources["FluentIcon.CalendarLtr28"],
                        FontFamily = "FluentRegular",
                        Color = (Color)Application.Current.Resources["IconAccentColor"]
                    }
                    : new FontImageSource 
                    { 
                        Glyph = (string)Application.Current.Resources["FluentIcon.Map24"],
                        FontFamily = "FluentRegular",
                        Color = (Color)Application.Current.Resources["IconAccentColor"]
                    };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
