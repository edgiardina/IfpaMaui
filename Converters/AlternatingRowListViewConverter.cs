using System.Globalization;

namespace Ifpa.Converters
{
    public class AlternatingRowListViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Colors.White;
            return 
                ((ListView)parameter).ItemsSource.Cast<object>().ToList().IndexOf(value) % 2 == 0 ?
                    (Color)Application.Current.Resources["BackgroundColor"] :
                    (Color)Application.Current.Resources["SecondaryBackgroundColor"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}