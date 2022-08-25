using System.Globalization;

namespace Ifpa.Converters
{
    public class AlternatingRowListViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Colors.White;

            var colorDictionary = Application.Current.Resources.MergedDictionaries.First();

            string darkStr = "";

            if (Application.Current.RequestedTheme == AppTheme.Dark)
                darkStr = "Dark";

            return 
                ((ListView)parameter).ItemsSource.Cast<object>().ToList().IndexOf(value) % 2 == 0 ?
                    (Color)colorDictionary["BackgroundColor" + darkStr] :
                    (Color)colorDictionary["SecondaryBackgroundColor" + darkStr];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}