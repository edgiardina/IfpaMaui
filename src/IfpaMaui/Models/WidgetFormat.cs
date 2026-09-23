using System.Globalization;

namespace Ifpa.Models
{
    /// <summary>
    /// How the Android widgets show an IFPA number, the same as the iOS widget
    /// does. A missing value shows as a dash, never as 0.
    /// </summary>
    public static class WidgetFormat
    {
        public const string Missing = "-";

        /// <summary>112 -> "112th", 1 -> "1st", 11 -> "11th"</summary>
        public static string Ordinal(long? value)
        {
            if (value is not { } number || number <= 0)
                return Missing;

            var suffix = (number % 100) switch
            {
                11 or 12 or 13 => "th",
                _ => (number % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th",
                },
            };
            return $"{number}{suffix}";
        }

        /// <summary>1024.61 -> "1,024.61", 54.5 -> "54.5"</summary>
        public static string Points(double? value, CultureInfo culture)
            => value is { } points ? points.ToString("#,##0.##", culture) : Missing;

        /// <summary>54.5 -> "54.5%"</summary>
        public static string Percent(double? value, CultureInfo culture)
            => value is { } percent ? percent.ToString("0.#", culture) + "%" : Missing;

        /// <summary>12345 -> "12,345"</summary>
        public static string Count(long? value, CultureInfo culture)
            => value is { } count ? count.ToString("#,##0", culture) : Missing;
    }
}
