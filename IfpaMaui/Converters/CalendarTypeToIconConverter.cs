using System.Globalization;
using MauiIcons.Fluent;
using MauiIcons.Core;
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
                    ? (FontImageSource)new MauiIcon() { Icon = FluentIcons.CalendarLtr28, IconColor = (Color)Application.Current.Resources["IconAccentColor"] }
                    : (FontImageSource)new MauiIcon() { Icon = FluentIcons.Map24, IconColor = (Color)Application.Current.Resources["IconAccentColor"] };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
