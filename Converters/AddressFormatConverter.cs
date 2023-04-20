using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Converters
{
    public class AddressFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            StringBuilder sb = new StringBuilder();

            bool hasCity = false;
            bool hasState = false;

            if (values == null || !targetType.IsAssignableFrom(typeof(string)))
            {
                return string.Empty;
                // Alternatively, return BindableProperty.UnsetValue to use the binding FallbackValue
            }

            //City
            if (values[0] is string && string.IsNullOrEmpty(values[0].ToString()) == false)
            {
                sb.Append(values[0]);
                hasCity = true;
            }

            //State
            if (values[1] is string && string.IsNullOrEmpty(values[1].ToString()) == false)
            {
                if (hasCity)
                {
                    sb.Append(", ");
                }

                sb.Append(values[1]);
                hasState = true;
            }

            //Country
            if (values[2] is string && string.IsNullOrEmpty(values[2].ToString()) == false)
            {
                if (hasState || hasCity)
                {
                    sb.Append(" ");
                }

                sb.Append(values[2]);
            }

            return sb.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
