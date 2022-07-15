using PinballApi.Extensions;
using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui;

namespace Ifpa.Converters
{
    public class PlayerListViewIndexConverter : BindableObject, IValueConverter
    {

        public int StartingRank
        {
            get
            {
                return (int)GetValue(StartingRankProperty);
            }
            set
            {
                SetValue(StartingRankProperty, value);
            }
        }

        public static BindableProperty StartingRankProperty =
                    BindableProperty.Create(nameof(StartingRank), typeof(int), typeof(int), 1);


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Colors.White;
            return (((ListView)parameter).ItemsSource.Cast<object>().ToList().IndexOf(value) + StartingRank).OrdinalSuffix();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
